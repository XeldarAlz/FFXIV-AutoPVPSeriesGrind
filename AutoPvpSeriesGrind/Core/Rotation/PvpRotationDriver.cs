using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;

namespace AutoPvpSeriesGrind.Core.Rotation;

internal sealed class PvpRotationDriver
{
    private const float GuardHpFraction = 0.15f;
    private const float GuardUnderBurstHpFraction = 0.45f;
    private const int GuardUnderBurstFocusCount = 2;
    private const uint RecuperateMissingHp = 15000;
    private const float ElixirResourceFraction = 1f / 3f;
    private const float ElixirSafeDistanceYalms = 25f;
    private const long MinTimeAliveMs = 5000;
    private const float WeaveMinGcdRemainingSec = 0.7f;
    private const float AllySupportHpFraction = 0.6f;

    private PvpJobKit? kit;
    private long aliveSinceMs;
    private uint lastUsedActionId;

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

        if (TryPurify(self))
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

        var enemy = ResolveEnemy(preferredTargetId, snapshot);
        var gcdRemaining = ActionOps.RecastRemainingSeconds(currentKit.GcdCooldownGroup);
        if (gcdRemaining <= 0f)
        {
            if (TryUseAny(currentKit.CooldownGcds, self, enemy, snapshot, mayStandStill, holdStill, out var gcdOutcome)
                || TryUseAny(currentKit.FillerGcds, self, enemy, snapshot, mayStandStill, holdStill, out gcdOutcome))
            {
                return gcdOutcome;
            }
        }
        else if (gcdRemaining < WeaveMinGcdRemainingSec)
        {
            return RotationOutcome.None;
        }

        return TryUseAny(currentKit.Abilities, self, enemy, snapshot, mayStandStill, holdStill, out var abilityOutcome)
            ? abilityOutcome
            : RotationOutcome.None;
    }

    private PvpJobKit KitFor(IPlayerCharacter self)
    {
        var jobId = self.ClassJob.RowId;
        if (kit is null || kit.JobId != jobId)
        {
            kit = PvpActionCatalog.For((Job)jobId);
        }
        return kit;
    }

    private bool TryPurify(IPlayerCharacter self)
        => MatchState.HasAnyStatus(self, PvpStatuses.PurifyClears) && UseOnSelf(self, PvpActions.Purify, "Purify");

    private bool TryGuard(IPlayerCharacter self, PvpSnapshot snapshot, bool underBurst, long aliveMs)
    {
        if (aliveMs < MinTimeAliveMs || MatchState.HasAnyStatus(self, PvpStatuses.GuardForbiddenBy))
        {
            return false;
        }

        var critical = snapshot.SelfHp <= GuardHpFraction;
        var burstFocused = underBurst && snapshot.SelfHp <= GuardUnderBurstHpFraction && snapshot.FocusCount >= GuardUnderBurstFocusCount;
        if (!critical && !burstFocused)
        {
            return false;
        }

        return UseOnSelf(self, PvpActions.Guard, critical ? "Guard (critical hp)" : "Guard (burst, focused)");
    }

    private bool TryRecuperate(IPlayerCharacter self, long aliveMs)
    {
        if (aliveMs < MinTimeAliveMs || self.MaxHp - self.CurrentHp < RecuperateMissingHp)
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

        var lowMp = self.CurrentMp <= self.MaxMp * ElixirResourceFraction;
        var lowHp = self.CurrentHp <= self.MaxHp * ElixirResourceFraction;
        if (!lowMp && !lowHp)
        {
            return false;
        }
        if (!ActionOps.IsReady(PvpActions.StandardIssueElixir, self.GameObjectId))
        {
            return false;
        }

        holdStill();
        return UseOnSelf(self, PvpActions.StandardIssueElixir, "Standard-issue Elixir");
    }

    private bool UseOnSelf(IPlayerCharacter self, uint actionId, string label)
    {
        if (!ActionOps.IsReady(actionId, self.GameObjectId) || !ActionOps.UseAction(actionId, self.GameObjectId))
        {
            return false;
        }

        lastUsedActionId = actionId;
        ApsgLog.Debug($"rotation: {label}");
        return true;
    }

    private bool TryUseAny(PvpActionInfo[] actions, IPlayerCharacter self, IGameObject? enemy, PvpSnapshot snapshot,
        bool mayStandStill, Action holdStill, out RotationOutcome outcome)
    {
        for (var actionIndex = 0; actionIndex < actions.Length; actionIndex++)
        {
            var action = actions[actionIndex];
            if (action.HasCastTime && !mayStandStill)
            {
                continue;
            }

            var target = PickTarget(action, self, enemy, snapshot);
            if (target is null)
            {
                continue;
            }

            var adjustedId = ActionOps.Adjusted(action.Id);
            if (!ActionOps.InRangeAndSight(adjustedId, target) || !ActionOps.IsReady(adjustedId, target.GameObjectId))
            {
                continue;
            }

            if (action.HasCastTime)
            {
                holdStill();
            }

            var used = action.TargetArea
                ? ActionOps.UseActionAt(adjustedId, target.GameObjectId, target.Position)
                : ActionOps.UseAction(adjustedId, target.GameObjectId);
            if (!used)
            {
                continue;
            }

            lastUsedActionId = action.Id;
            ApsgLog.Debug($"rotation: {action.Name} -> {target.Name}");
            outcome = action.HasCastTime ? RotationOutcome.Cast : RotationOutcome.Instant;
            return true;
        }

        outcome = RotationOutcome.None;
        return false;
    }

    private static IGameObject? PickTarget(in PvpActionInfo action, IPlayerCharacter self, IGameObject? enemy, PvpSnapshot snapshot)
    {
        if (action.TargetsHostile)
        {
            return enemy;
        }
        if (action.IsAllySupport)
        {
            return LowestAllyBelow(snapshot, AllySupportHpFraction) ?? (snapshot.SelfHp <= AllySupportHpFraction && action.TargetsSelf ? self : null);
        }
        return self;
    }

    private static IGameObject? LowestAllyBelow(PvpSnapshot snapshot, float hpFraction)
    {
        var lowestId = 0UL;
        var lowestHp = hpFraction;
        for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
        {
            var ally = snapshot.Allies[allyIndex];
            if (ally.Hp < lowestHp)
            {
                lowestHp = ally.Hp;
                lowestId = ally.Id;
            }
        }
        return lowestId == 0 ? null : Svc.Objects.SearchById(lowestId);
    }

    private static IGameObject? ResolveEnemy(ulong preferredTargetId, PvpSnapshot snapshot)
    {
        var targetId = preferredTargetId != 0 ? preferredTargetId : NearestEnemyId(snapshot);
        return targetId == 0 ? null : Svc.Objects.SearchById(targetId);
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
