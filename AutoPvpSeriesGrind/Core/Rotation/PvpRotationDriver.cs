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

    private static readonly RuleCondition[] NoConditions = [];

    private readonly record struct RuleContext(
        IPlayerCharacter Self,
        IBattleChara? Enemy,
        PvpSnapshot Snapshot,
        bool MayStandStill,
        float AllySupportHp,
        Action HoldStill);

    private RotationSettings settings;
    private PvpJobKit? kit;
    private RotationTable? table;
    private long aliveSinceMs;
    private uint lastUsedActionId;

    public void Configure(in RotationSettings rotationSettings) => settings = rotationSettings;

    public void OnAlive()
    {
        aliveSinceMs = Environment.TickCount64;
        lastUsedActionId = 0;
    }

    public RotationOutcome Tick(PvpSnapshot snapshot, ulong preferredTargetId, bool underBurst, bool mayStandStill, Action holdStill)
    {
        if (Svc.Objects.LocalPlayer is not { } self || self.IsCasting || ActionOps.AnimationLocked)
        {
            return RotationOutcome.None;
        }

        if (MatchState.HasStatus(self, PvpStatuses.Guard))
        {
            return RotationOutcome.Guarding;
        }

        var currentKit = KitFor(self);
        var aliveMs = Environment.TickCount64 - aliveSinceMs;

        if (settings.Purify && TryPurify(self))
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
        if (TryElixir(self, snapshot, aliveMs, holdStill))
        {
            return RotationOutcome.Cast;
        }

        var gcdRemaining = ActionOps.RecastRemainingSeconds(currentKit.GcdCooldownGroup);
        var gcdReady = gcdRemaining <= 0f;
        var canWeave = gcdRemaining >= WeaveMinGcdRemainingSec;
        if (!gcdReady && !canWeave)
        {
            return RotationOutcome.None;
        }

        var context = new RuleContext(self, ResolveEnemy(preferredTargetId, snapshot), snapshot, mayStandStill, settings.AllySupportHp, holdStill);
        return table is not null
            ? RunTable(table, in context, gcdReady)
            : RunGeneric(currentKit, in context, gcdReady);
    }

    private PvpJobKit KitFor(IPlayerCharacter self)
    {
        var jobId = self.ClassJob.RowId;
        if (kit is null || kit.JobId != jobId)
        {
            kit = PvpActionCatalog.For((Job)jobId);
            table = JobRotationTables.TryGet(jobId, out var jobTable) ? jobTable : null;
        }
        return kit;
    }

    private RotationOutcome RunTable(RotationTable rotationTable, in RuleContext context, bool gcdReady)
    {
        if (rotationTable.PauseWhileSelfHas != 0 && MatchState.HasStatus(context.Self, rotationTable.PauseWhileSelfHas))
        {
            return RotationOutcome.None;
        }

        if (gcdReady && TryRules(rotationTable.Rules, in context, wantGcd: true, out var outcome))
        {
            return outcome;
        }
        return TryRules(rotationTable.Rules, in context, wantGcd: false, out outcome) ? outcome : RotationOutcome.None;
    }

    private RotationOutcome RunGeneric(PvpJobKit currentKit, in RuleContext context, bool gcdReady)
    {
        if (gcdReady && TryButtons(currentKit.Buttons, in context, wantGcd: true, out var outcome))
        {
            return outcome;
        }
        return TryButtons(currentKit.Buttons, in context, wantGcd: false, out outcome) ? outcome : RotationOutcome.None;
    }

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
        if (target is null || WastedOnImmunity(in info, adjustedId, target, in context))
        {
            return false;
        }
        if (!InRange(in info, adjustedId, target, context.Self) || !ActionOps.IsReady(adjustedId))
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
        ApsgLog.Debug($"rotation: {info.Name} -> {target.Name}");
        outcome = info.HasCastTime ? RotationOutcome.Cast : RotationOutcome.Instant;
        return true;
    }

    private static bool WastedOnImmunity(in PvpActionInfo info, uint adjustedId, IGameObject target, in RuleContext context)
    {
        if (!info.TargetsHostile || context.Enemy is not { } enemy || target.GameObjectId != enemy.GameObjectId)
        {
            return false;
        }
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
            default:
                return true;
        }
    }

    private static IGameObject? PickTarget(in PvpActionInfo info, RuleTarget targetRule, in RuleContext context)
    {
        switch (targetRule)
        {
            case RuleTarget.Self:
                return context.Self;
            case RuleTarget.NearestAlly:
                return NearestAlly(context.Snapshot);
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

    private bool TryPurify(IPlayerCharacter self)
        => MatchState.HasAnyStatus(self, PvpStatuses.PurifyClears) && UseOnSelf(self, PvpActions.Purify, "Purify");

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
        ApsgLog.Debug($"rotation: {label}");
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
        return targetId == 0 ? null : Svc.Objects.SearchById(targetId) as IBattleChara;
    }

    private static ulong NearestEnemyId(PvpSnapshot snapshot)
    {
        var nearestId = 0UL;
        var nearestDistance = float.MaxValue;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.DistanceToSelf < nearestDistance)
            {
                nearestDistance = enemy.DistanceToSelf;
                nearestId = enemy.Id;
            }
        }
        return nearestId;
    }
}
