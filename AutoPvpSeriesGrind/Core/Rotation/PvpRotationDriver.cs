using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Rotation;

internal sealed class PvpRotationDriver
{
    private const int GuardUnderBurstFocusCount = 2;
    private const float ElixirSafeDistanceYalms = 25f;
    private const long MinTimeAliveMs = 5000;
    private const float WeaveMinGcdRemainingSec = 0.7f;
    private const float GcdQueueWindowSec = 0.4f;
    private const float GapCloseMinYalms = 8f;
    private const float GapCloseMaxYalms = 20f;
    private const float EscapeTriggerYalms = 8f;
    private const float AllyDashMinYalms = 8f;
    private const float AllyDashMaxYalms = 25f;

    private static readonly RuleCondition[] NoConditions = [];

    private readonly record struct RuleContext(
        IPlayerCharacter Self,
        IBattleChara? Enemy,
        PvpSnapshot Snapshot,
        bool MayStandStill,
        float AllySupportHp,
        float GcdRemaining,
        byte GcdCooldownGroup,
        Action HoldStill);

    private RotationSettings settings;
    private PvpJobKit? kit;
    private RotationTable? table;
    private MovementKit movement;
    private long aliveSinceMs;
    private uint lastUsedActionId;

    public void Configure(in RotationSettings rotationSettings) => settings = rotationSettings;

    public void OnAlive()
    {
        aliveSinceMs = Environment.TickCount64;
        lastUsedActionId = 0;
    }

    public RotationOutcome Tick(PvpSnapshot snapshot, ulong preferredTargetId, bool underBurst, Posture posture, Vector3 moveDestination, Action holdStill)
    {
        if (Svc.Objects.LocalPlayer is not { } self || ActionOps.AnimationLocked || Svc.Condition[ConditionFlag.PilotingMech])
        {
            return RotationOutcome.None;
        }
        if (self.IsCasting)
        {
            CancelWastedCast(self);
            return RotationOutcome.None;
        }

        if (MatchState.HasStatus(self, PvpStatuses.Guard))
        {
            return RotationOutcome.Guarding;
        }

        var currentKit = KitFor(self);
        var aliveMs = Environment.TickCount64 - aliveSinceMs;

        if (TryCleanse(self))
        {
            return RotationOutcome.Instant;
        }
        if (TryGuard(self, snapshot, underBurst, aliveMs))
        {
            return RotationOutcome.Guarding;
        }
        if (TryRecuperate(self, aliveMs))
        {
            return RotationOutcome.Instant;
        }
        if (posture != Posture.Idle && TryElixir(self, snapshot, aliveMs, holdStill))
        {
            return RotationOutcome.Cast;
        }

        var gcdRemaining = ActionOps.RecastRemainingSeconds(currentKit.GcdCooldownGroup);
        var gcdReady = gcdRemaining <= GcdQueueWindowSec && !ActionOps.HasQueuedAction;
        var canWeave = gcdRemaining >= WeaveMinGcdRemainingSec;
        var mayStandStill = posture is Posture.Hold or Posture.Push or Posture.Stage;
        var context = new RuleContext(self, ResolveEnemy(preferredTargetId, snapshot), snapshot, mayStandStill, settings.AllySupportHp,
            gcdRemaining, currentKit.GcdCooldownGroup, holdStill);

        if (TryEscape(in context, posture, moveDestination))
        {
            return RotationOutcome.Instant;
        }
        if (!gcdReady && !canWeave)
        {
            return RotationOutcome.None;
        }
        if (canWeave && TryGapClose(in context, posture))
        {
            return RotationOutcome.Instant;
        }

        return RunRotation(currentKit, in context, gcdReady);
    }

    private static void CancelWastedCast(IPlayerCharacter self)
    {
        if (Svc.Objects.SearchById(self.CastTargetObjectId) is not IBattleChara castTarget
            || castTarget.GameObjectId == self.GameObjectId
            || !MatchState.IsEnemyPlayer(castTarget))
        {
            return;
        }

        var dead = castTarget.CurrentHp == 0;
        var immune = MatchState.ShouldAvoidOffense(castTarget)
                     || (MatchState.HasAnyStatus(castTarget, PvpStatuses.DamageImmunities)
                         && Array.IndexOf(PvpActions.WorthUsingIntoGuard, self.CastActionId) < 0);
        if (!dead && !immune)
        {
            return;
        }

        ActionOps.CancelCast();
        RunLog.Debug(dead ? "cast cancelled, target died" : "cast cancelled, target became immune");
    }

    private bool TryEscape(in RuleContext context, Posture posture, Vector3 moveDestination)
    {
        if (posture is not (Posture.Retreat or Posture.Regroup or Posture.Reposition))
        {
            return false;
        }
        if (context.Snapshot.NearestEnemyDistance > EscapeTriggerYalms)
        {
            return false;
        }

        if (NearestAlly(context.Snapshot) is { } ally)
        {
            var allyDistance = Vector3.Distance(context.Self.Position, ally.Position);
            if (allyDistance >= AllyDashMinYalms && allyDistance <= AllyDashMaxYalms && TryDashToward(movement.AllyDashes, ally, in context))
            {
                return true;
            }
        }

        return TryDashForward(movement.ForwardDashes, moveDestination, in context);
    }

    private bool TryGapClose(in RuleContext context, Posture posture)
    {
        if (posture != Posture.Push || context.Enemy is not { } enemy)
        {
            return false;
        }

        var distance = Vector3.Distance(context.Self.Position, enemy.Position);
        if (distance < GapCloseMinYalms || distance > GapCloseMaxYalms || MatchState.HasAnyStatus(enemy, PvpStatuses.DamageImmunities))
        {
            return false;
        }

        return TryDashToward(movement.GapClosers, enemy, in context) || TryDashForward(movement.ForwardDashes, enemy.Position, in context);
    }

    private bool TryDashToward(uint[] dashes, IGameObject target, in RuleContext context)
    {
        for (var dashIndex = 0; dashIndex < dashes.Length; dashIndex++)
        {
            var actionId = dashes[dashIndex];
            if (!PvpActionCatalog.TryGetInfo(actionId, out var info) || !ActionOps.IsReady(actionId))
            {
                continue;
            }
            if (!InRange(in info, actionId, target, context.Self))
            {
                continue;
            }

            var used = info.TargetArea
                ? ActionOps.UseActionAt(actionId, context.Self.GameObjectId, target.Position)
                : ActionOps.UseAction(actionId, target.GameObjectId);
            if (used)
            {
                lastUsedActionId = actionId;
                RunLog.Debug($"dash {info.Name} -> {target.Name}");
                return true;
            }
        }
        return false;
    }

    private bool TryDashForward(uint[] dashes, Vector3 toward, in RuleContext context)
    {
        var self = context.Self;
        var direction = toward - self.Position;
        var distance = direction.Length();
        if (distance < GapCloseMinYalms)
        {
            return false;
        }

        for (var dashIndex = 0; dashIndex < dashes.Length; dashIndex++)
        {
            var actionId = dashes[dashIndex];
            if (!PvpActionCatalog.TryGetInfo(actionId, out var info) || !ActionOps.IsReady(actionId))
            {
                continue;
            }

            var used = info.TargetArea
                ? ActionOps.UseActionAt(actionId, self.GameObjectId, self.Position + direction / distance * MathF.Min(info.Range, distance))
                : ActionOps.UseAction(actionId, self.GameObjectId);
            if (used)
            {
                lastUsedActionId = actionId;
                RunLog.Debug($"dash {info.Name}");
                return true;
            }
        }
        return false;
    }

    private PvpJobKit KitFor(IPlayerCharacter self)
    {
        var jobId = self.ClassJob.RowId;
        if (kit is null || kit.JobId != jobId)
        {
            kit = PvpActionCatalog.For((Job)jobId);
            table = JobRotationTables.TryGet(jobId, out var jobTable) ? jobTable : null;
            movement = MovementAssist.TryGet(jobId, out var movementKit) ? movementKit : new MovementKit([], [], []);
        }
        return kit;
    }

    // The role action goes first in each phase: its rules only let it through when it pays off (Smite on a low enemy,
    // Bloodbath when hurt), and Comet is a caster's strongest GCD.
    private RotationOutcome RunRotation(PvpJobKit currentKit, in RuleContext context, bool gcdReady)
    {
        if (table is { PauseWhileSelfHas: not 0 } && MatchState.HasStatus(context.Self, table.PauseWhileSelfHas))
        {
            return RotationOutcome.None;
        }

        var roleRules = RoleActionRules.For(ActionOps.Adjusted(PvpActions.RoleAction));
        if (gcdReady && (TryRules(roleRules, in context, wantGcd: true, out var outcome) || TryJob(currentKit, in context, wantGcd: true, out outcome)))
        {
            return outcome;
        }
        return TryRules(roleRules, in context, wantGcd: false, out outcome) || TryJob(currentKit, in context, wantGcd: false, out outcome)
            ? outcome
            : RotationOutcome.None;
    }

    private bool TryJob(PvpJobKit currentKit, in RuleContext context, bool wantGcd, out RotationOutcome outcome)
        => table is not null
            ? TryRules(table.Rules, in context, wantGcd, out outcome)
            : TryButtons(currentKit.Buttons, in context, wantGcd, out outcome);

    private bool TryRules(RotationRule[] rules, in RuleContext context, bool wantGcd, out RotationOutcome outcome)
    {
        for (var ruleIndex = 0; ruleIndex < rules.Length; ruleIndex++)
        {
            ref readonly var rule = ref rules[ruleIndex];
            if (TryButton(rule.Button, rule.Adjusted, rule.Target, rule.Conditions, in context, wantGcd, out outcome))
            {
                return true;
            }
        }

        outcome = RotationOutcome.None;
        return false;
    }

    private bool TryButtons(PvpActionInfo[] buttons, in RuleContext context, bool wantGcd, out RotationOutcome outcome)
    {
        for (var buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
        {
            if (TryButton(buttons[buttonIndex].Id, 0, RuleTarget.Auto, NoConditions, in context, wantGcd, out outcome))
            {
                return true;
            }
        }

        outcome = RotationOutcome.None;
        return false;
    }

    private bool TryButton(uint button, uint requiredAdjusted, RuleTarget targetRule, RuleCondition[] conditions,
        in RuleContext context, bool wantGcd, out RotationOutcome outcome)
    {
        outcome = RotationOutcome.None;
        var adjustedId = ActionOps.Adjusted(button);
        if (requiredAdjusted != 0 && adjustedId != requiredAdjusted)
        {
            return false;
        }
        if (!PvpActionCatalog.TryGetInfo(adjustedId, out var info) && !PvpActionCatalog.TryGetInfo(button, out info))
        {
            return false;
        }
        if (info.IsGcd != wantGcd || (info.HasCastTime && !context.MayStandStill))
        {
            return false;
        }
        if (!ConditionsHold(conditions, in context, adjustedId))
        {
            return false;
        }

        var target = PickTarget(in info, targetRule, in context);
        if (target is null || WastedOnImmunity(in info, adjustedId, target, context.Self))
        {
            return false;
        }
        if (!InRange(in info, adjustedId, target, context.Self) || !IsReadyOrQueueable(in info, adjustedId, in context))
        {
            return false;
        }

        if (info.HasCastTime)
        {
            context.HoldStill();
        }

        var used = info.TargetArea
            ? ActionOps.UseActionAt(adjustedId, context.Self.GameObjectId, target.Position)
            : ActionOps.UseAction(adjustedId, target.GameObjectId);
        if (!used)
        {
            return false;
        }

        lastUsedActionId = adjustedId;
        RunLog.Debug($"{info.Name} -> {target.Name}");
        outcome = info.HasCastTime ? RotationOutcome.Cast : RotationOutcome.Instant;
        return true;
    }

    private static bool IsReadyOrQueueable(in PvpActionInfo info, uint adjustedId, in RuleContext context)
    {
        if (ActionOps.IsReady(adjustedId))
        {
            return true;
        }

        var plainGcdInWindow = info.IsGcd
                               && info.CooldownGroup == context.GcdCooldownGroup
                               && context.GcdRemaining > 0f
                               && context.GcdRemaining <= GcdQueueWindowSec;
        return plainGcdInWindow && ActionOps.IsReadyIgnoringRecast(adjustedId);
    }

    private static bool WastedOnImmunity(in PvpActionInfo info, uint adjustedId, IGameObject target, IPlayerCharacter self)
    {
        if (!info.TargetsHostile || target is not IBattleChara enemy || enemy.GameObjectId == self.GameObjectId)
        {
            return false;
        }
        if (MatchState.ShouldAvoidOffense(enemy)) return true;
        if (Array.IndexOf(PvpActions.WorthUsingIntoGuard, adjustedId) >= 0)
        {
            return false;
        }
        return MatchState.HasAnyStatus(enemy, PvpStatuses.DamageImmunities);
    }

    private static bool InRange(in PvpActionInfo info, uint adjustedId, IGameObject target, IPlayerCharacter self)
    {
        if (target.GameObjectId == self.GameObjectId || info.Range <= 0)
        {
            return true;
        }
        if (info.TargetArea)
        {
            return Vector3.Distance(self.Position, target.Position) <= info.Range;
        }
        return ActionOps.InRangeAndSight(adjustedId, target);
    }

    private bool ConditionsHold(RuleCondition[] conditions, in RuleContext context, uint adjustedId)
    {
        for (var conditionIndex = 0; conditionIndex < conditions.Length; conditionIndex++)
        {
            if (!Holds(in conditions[conditionIndex], in context, adjustedId))
            {
                return false;
            }
        }
        return true;
    }

    private bool Holds(in RuleCondition condition, in RuleContext context, uint adjustedId)
    {
        var self = context.Self;
        var enemy = context.Enemy;
        var snapshot = context.Snapshot;
        switch (condition.Kind)
        {
            case RuleWhen.SelfHasStatus:
                return MatchState.HasStatus(self, condition.Status);
            case RuleWhen.SelfLacksStatus:
                return !MatchState.HasStatus(self, condition.Status);
            case RuleWhen.SelfHasAnyStatus:
                return MatchState.HasAnyStatus(self, condition.Statuses!);
            case RuleWhen.TargetHasStatus:
                return enemy is not null && MatchState.HasStatus(enemy, condition.Status);
            case RuleWhen.TargetLacksStatus:
                return enemy is not null && !MatchState.HasStatus(enemy, condition.Status);
            case RuleWhen.SelfHpBelow:
                return snapshot.SelfHp < condition.Value;
            case RuleWhen.SelfHpAbove:
                return snapshot.SelfHp > condition.Value;
            case RuleWhen.TargetHpBelow:
                return enemy is not null && HpFraction(enemy) <= condition.Value;
            case RuleWhen.EnemiesWithin:
                return snapshot.EnemiesWithin(condition.Value) > 0;
            case RuleWhen.InCombat:
                return Svc.Condition[ConditionFlag.InCombat];
            case RuleWhen.SelfStatusEndingWithin:
                var remaining = MatchState.StatusRemainingSeconds(self, condition.Status);
                return remaining > 0f && remaining <= condition.Value;
            case RuleWhen.SelfStatusStacksAtLeast:
                return MatchState.StatusStacks(self, condition.Status) >= (int)condition.Value;
            case RuleWhen.SelfStatusStacksAtMost:
                var stacks = MatchState.StatusStacks(self, condition.Status);
                return stacks > 0 && stacks <= (int)condition.Value;
            case RuleWhen.ChargesAtLeast:
                return ActionOps.CurrentCharges(adjustedId) >= (uint)condition.Value;
            case RuleWhen.NotLastUsed:
                return lastUsedActionId != adjustedId;
            case RuleWhen.TargetWithin:
                return enemy is not null && Vector3.Distance(self.Position, enemy.Position) <= condition.Value;
            case RuleWhen.TargetBeyond:
                return enemy is not null && Vector3.Distance(self.Position, enemy.Position) > condition.Value;
            case RuleWhen.AllyBelow:
                return LowestAllyIdWithin(snapshot, condition.Value, condition.Range) != 0;
            case RuleWhen.CanFollowWith:
                return enemy is not null && CanFollowWith(condition.Button, enemy, self);
            case RuleWhen.Recasting:
                return PvpActionCatalog.TryGetInfo(condition.Button, out var recastInfo) && ActionOps.RecastRemainingSeconds(recastInfo.CooldownGroup) > 0f;
            case RuleWhen.StandingStill:
                return context.MayStandStill;
            case RuleWhen.OnTheMove:
                return !context.MayStandStill;
            case RuleWhen.EnemyInReachBelow:
                return WeakestEnemyIdWithin(snapshot, condition.Value, ReachOf(adjustedId)) != 0;
            default:
                return true;
        }
    }

    private static bool CanFollowWith(uint button, IBattleChara enemy, IPlayerCharacter self)
    {
        var followUpId = ActionOps.Adjusted(button);
        return PvpActionCatalog.TryGetInfo(followUpId, out var info)
               && ActionOps.CurrentCharges(followUpId) >= 1
               && !MatchState.HasAnyStatus(enemy, PvpStatuses.DamageImmunities)
               && InRange(in info, followUpId, enemy, self);
    }

    private static IGameObject? PickTarget(in PvpActionInfo info, RuleTarget targetRule, in RuleContext context)
    {
        switch (targetRule)
        {
            case RuleTarget.Self:
                return context.Self;
            case RuleTarget.NearestAlly:
                return NearestAlly(context.Snapshot);
            case RuleTarget.WeakestEnemy:
            {
                var weakestId = WeakestEnemyIdWithin(context.Snapshot, 1f, ReachOf(info.Id));
                return weakestId == 0 ? null : Svc.Objects.SearchById(weakestId);
            }
        }

        if (info.TargetsHostile)
        {
            return context.Enemy;
        }
        if (info.TargetsAlly)
        {
            var reach = info.Range > 0 ? info.Range : float.MaxValue;
            var allyId = LowestAllyIdWithin(context.Snapshot, context.AllySupportHp, reach);
            if (allyId != 0)
            {
                return Svc.Objects.SearchById(allyId);
            }
            return info.TargetsSelf && context.Snapshot.SelfHp <= context.AllySupportHp ? context.Self : null;
        }
        if (info.TargetsSelf)
        {
            return context.Self;
        }
        if (info.TargetArea)
        {
            return info.Range == 0 ? context.Self : context.Enemy;
        }
        return null;
    }

    // The job's own cleanse (Warden's Paean, Aquaveil, Meikyo Shisui) goes first and covers slows and roots too;
    // Purify is the fallback and only for crowd control that stops you acting.
    private bool TryCleanse(IPlayerCharacter self)
    {
        if (!MatchState.HasAnyStatus(self, PvpStatuses.PurifyClears))
        {
            return false;
        }
        if (table is { Cleanse.Button: not 0 } && TryOwnCleanse(self, table.Cleanse))
        {
            return true;
        }

        return settings.Purify && MatchState.HasAnyStatus(self, PvpStatuses.PurifyWorthy) && UseOnSelf(self, PvpActions.Purify, "Purify");
    }

    private bool TryOwnCleanse(IPlayerCharacter self, in RotationRule cleanse)
    {
        var adjustedId = ActionOps.Adjusted(cleanse.Button);
        if (cleanse.Adjusted != 0 && adjustedId != cleanse.Adjusted)
        {
            return false;
        }

        return UseOnSelf(self, adjustedId, PvpActionCatalog.TryGetInfo(adjustedId, out var info) ? info.Name : "cleanse");
    }

    private bool TryGuard(IPlayerCharacter self, PvpSnapshot snapshot, bool underBurst, long aliveMs)
    {
        if (aliveMs < MinTimeAliveMs || MatchState.HasAnyStatus(self, PvpStatuses.GuardForbiddenBy))
        {
            return false;
        }

        var critical = snapshot.SelfHp <= settings.GuardHp;
        var burstFocused = settings.GuardOnBurst && underBurst && snapshot.SelfHp <= settings.GuardOnBurstHp && snapshot.FocusCount >= GuardUnderBurstFocusCount;
        if (!critical && !burstFocused)
        {
            return false;
        }

        return UseOnSelf(self, PvpActions.Guard, critical ? "Guard (critical hp)" : "Guard (burst, focused)");
    }

    private bool TryRecuperate(IPlayerCharacter self, long aliveMs)
    {
        if (aliveMs < MinTimeAliveMs || self.MaxHp - self.CurrentHp < settings.RecuperateMissingHp)
        {
            return false;
        }

        return UseOnSelf(self, PvpActions.Recuperate, "Recuperate");
    }

    private bool TryElixir(IPlayerCharacter self, PvpSnapshot snapshot, long aliveMs, Action holdStill)
    {
        if (aliveMs < MinTimeAliveMs || lastUsedActionId == PvpActions.StandardIssueElixir)
        {
            return false;
        }
        if (snapshot.NearestEnemyDistance <= ElixirSafeDistanceYalms)
        {
            return false;
        }

        var lowMp = self.CurrentMp <= self.MaxMp * settings.ElixirResourceFraction;
        var lowHp = self.CurrentHp <= self.MaxHp * settings.ElixirResourceFraction;
        if (!lowMp && !lowHp)
        {
            return false;
        }
        if (!ActionOps.IsReady(PvpActions.StandardIssueElixir))
        {
            return false;
        }

        holdStill();
        return UseOnSelf(self, PvpActions.StandardIssueElixir, "Standard-issue Elixir");
    }

    private bool UseOnSelf(IPlayerCharacter self, uint actionId, string label)
    {
        if (!ActionOps.IsReady(actionId) || !ActionOps.UseAction(actionId, self.GameObjectId))
        {
            return false;
        }

        lastUsedActionId = actionId;
        RunLog.Debug($"{label}");
        return true;
    }

    private static float HpFraction(IBattleChara chara)
        => chara.MaxHp > 0 ? (float)chara.CurrentHp / chara.MaxHp : 1f;

    private static ulong LowestAllyIdWithin(PvpSnapshot snapshot, float hpFraction, float withinYalms)
    {
        var lowestId = 0UL;
        var lowestHp = hpFraction;
        for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
        {
            var ally = snapshot.Allies[allyIndex];
            if (ally.Hp < lowestHp && ally.DistanceToSelf <= withinYalms)
            {
                lowestHp = ally.Hp;
                lowestId = ally.Id;
            }
        }
        return lowestId;
    }

    private static float ReachOf(uint actionId)
        => PvpActionCatalog.TryGetInfo(actionId, out var info) && info.Range > 0 ? info.Range : float.MaxValue;

    private static ulong WeakestEnemyIdWithin(PvpSnapshot snapshot, float hpFraction, float withinYalms)
    {
        var weakestId = 0UL;
        var weakestHp = float.MaxValue;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.CanAttack && enemy.Hp <= hpFraction && enemy.Hp < weakestHp && enemy.DistanceToSelf <= withinYalms)
            {
                weakestHp = enemy.Hp;
                weakestId = enemy.Id;
            }
        }
        return weakestId;
    }

    private static IGameObject? NearestAlly(PvpSnapshot snapshot)
    {
        var nearestId = 0UL;
        var nearestDistance = float.MaxValue;
        for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
        {
            var ally = snapshot.Allies[allyIndex];
            if (ally.DistanceToSelf < nearestDistance)
            {
                nearestDistance = ally.DistanceToSelf;
                nearestId = ally.Id;
            }
        }
        return nearestId == 0 ? null : Svc.Objects.SearchById(nearestId);
    }

    private static IBattleChara? ResolveEnemy(ulong preferredTargetId, PvpSnapshot snapshot)
    {
        var targetId = preferredTargetId != 0 ? preferredTargetId : NearestEnemyId(snapshot);
        var enemy = targetId == 0 ? null : Svc.Objects.SearchById(targetId) as IBattleChara;
        return enemy is not null && !MatchState.ShouldAvoidOffense(enemy) && MatchState.IsEnemyPlayer(enemy) ? enemy : null;
    }

    private static ulong NearestEnemyId(PvpSnapshot snapshot)
    {
        var nearestId = 0UL;
        var nearestDistance = float.MaxValue;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.CanAttack && enemy.DistanceToSelf < nearestDistance)
            {
                nearestDistance = enemy.DistanceToSelf;
                nearestId = enemy.Id;
            }
        }
        return nearestId;
    }
}
