using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Util;
using ECommons.DalamudServices;
using System.Numerics;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries
{
    private const float CrystalEngageRadiusYalms = 10f;
    private const float LegacyCrystalStopRange = 1.5f;

    private const float SpawnExitArrivalRange = 3.5f;
    private const float ObjectiveFallbackStopRange = 3f;
    private const int SpawnExitTimeoutMs = 8000;

    private async Task TickLiveMatch()
    {
        rotation.TickDeathAndRespawn();
        if (IsDead())
        {
            rotation.OnDeadDuringLive();
            movement.Stop();
            matchFlow.LeftSpawn = false;
            matchFlow.LeaveSpawnStartedAtMs = 0;
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Retreat, "dead, waiting to respawn", Posture.Retreat);
            return;
        }

        rotation.EnsureSignCleared();
        rotation.EnsureRotationEnabled();

        var territory = Svc.ClientState.TerritoryType;

        if (MatchState.HasStatus(StatusSpawnProtection))
        {
            MovementExecutor.EnsureSprinting();
            matchFlow.RanSafetyMoveThisDuty = true;
            CaptureBasesAtSpawn(territory);
        }

        if (MatchState.LocalIsCasting(ActionStandardIssueElixir))
        {
            movement.Stop();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Hold, "wait: elixir cast (hp/mp refill)", Posture.Hold);
            return;
        }

        if (!matchFlow.LeftSpawn && !TryLeaveSpawn(territory))
            return;

        if (settings.EnableBrain)
            await RunBrainTick(territory);
        else
            LegacyCrystalMove();
    }

    private void CaptureBasesAtSpawn(uint territory)
    {
        if (matchFlow.Bases is null && MatchState.PlayerPosition() is { } spawnPosition)
        {
            matchFlow.Bases = MatchState.IdentifyBases(territory, spawnPosition);
            if (matchFlow.Bases is { } bases)
                LogDiagnostic($"bases identified: own={bases.Own:F0} enemy={bases.Enemy:F0}");
        }
    }

    private Vector3? SpawnExit(uint territory, Vector3 self)
        => matchFlow.Bases?.Own ?? MatchState.NearestSafeAnchor(territory, self);

    // Players are already pressed against the barrier when it drops. Requesting the gate path a few
    // seconds early hides pathfind latency, so the character is moving the moment the gate opens.
    private void ApproachGate(uint territory)
    {
        if (MatchState.PlayerPosition() is not { } self || SpawnExit(territory, self) is not { } exit)
        {
            return;
        }

        movement.IssueMove(exit, exit, SpawnExitArrivalRange);
        BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Engage, "gate opening soon, moving up", Posture.Reposition);
    }

    private bool TryLeaveSpawn(uint territory)
    {
        if (MatchState.PlayerPosition() is not { } self)
            return true;

        if (SpawnExit(territory, self) is not { } exit)
        {
            matchFlow.LeftSpawn = true;
            return true;
        }

        var now = Environment.TickCount64;
        var moveIssued = matchFlow.LeaveSpawnStartedAtMs != 0;
        if (!moveIssued)
        {
            matchFlow.LeaveSpawnStartedAtMs = now;
        }

        var flat = VectorMath.HorizontalDistance(self, exit);
        var arrived = flat <= SpawnExitArrivalRange;
        var pathEnded = moveIssued && !movement.IsPathing;
        var timedOut = now - matchFlow.LeaveSpawnStartedAtMs > SpawnExitTimeoutMs;
        if (arrived || pathEnded || timedOut)
        {
            matchFlow.LeftSpawn = true;
            LogDiagnostic(SpawnHandoffReason(arrived, pathEnded, flat));
            return true;
        }

        movement.IssueMove(exit, exit, SpawnExitArrivalRange);
        BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Engage, "leaving spawn, to gate anchor", Posture.Reposition);
        return false;
    }

    private static string SpawnHandoffReason(bool arrived, bool pathEnded, float flat)
    {
        if (arrived)
        {
            return $"off the spawn platform (anchor {flat:F1}y) -> brain takes over";
        }

        if (pathEnded)
        {
            return $"spawn exit path ended {flat:F1}y from anchor -> brain takes over";
        }

        return $"leave-spawn timeout ({SpawnExitTimeoutMs}ms, {flat:F1}y out) -> handing off to brain";
    }

    private async Task RunBrainTick(uint territory)
    {
        var snapshot = MatchState.Capture();

        if (!snapshot.HasObjective)
        {
            WarnMissingObjectiveOnce();
            AdvanceWithoutObjective(snapshot);
            return;
        }

        matchFlow.LoggedMissingObjective = false;

        var anchor = matchFlow.Bases?.Own ?? MatchState.NearestSafeAnchor(territory, snapshot.Self) ?? snapshot.Self;
        var plan = brain.Decide(snapshot, anchor, matchFlow.Bases?.Enemy);
        BrainTelemetry.Record(snapshot, plan);

        var planChanged = movement.UpdatePosture(plan.Posture);
        if (settings.Humanize != HumanizeLevel.Off && planChanged)
        {
            var (reactionMinMs, reactionMaxMs) = HumanTiming.ReactionBand(settings.Humanize);
            await NextFrame(HumanTiming.Reaction(reactionMinMs, reactionMaxMs));
        }

        ApplyBrainTarget(plan.TargetId);
        movement.Execute(plan);
    }

    // The crystal starts at the arena center, midway along the line joining the two bases. Standing
    // still until it resolves is what made the bot look parked at the spawn exit, so walk the line instead.
    private void AdvanceWithoutObjective(in PvpSnapshot snapshot)
    {
        if (matchFlow.Bases is not { } bases)
        {
            BrainTelemetry.Record(snapshot, new MovePlan(MoveKind.Hold, snapshot.Self, snapshot.Self, 0f, false, "no objective"));
            movement.HaltPathing();
            return;
        }

        var crystalLineCenter = Vector3.Lerp(bases.Own, bases.Enemy, 0.5f);
        BrainTelemetry.RecordStatus(snapshot, MoveKind.Engage, "no objective yet, advancing to crystal line", Posture.Reposition);
        movement.IssueMove(crystalLineCenter, bases.Own, ObjectiveFallbackStopRange);
    }

    private void WarnMissingObjectiveOnce()
    {
        if (matchFlow.LoggedMissingObjective)
        {
            return;
        }

        matchFlow.LoggedMissingObjective = true;
        Warn($"objective not found among {Svc.Objects.Length} loaded objects (Tactical Crystal, BNpcName {TacticalCrystalNameId})");
    }

    private void ApplyBrainTarget(ulong targetId)
    {
        if (settings.BrainTargets && targetId != 0)
        {
            MatchState.SetTarget(targetId);
        }
    }

    private void LegacyCrystalMove()
    {
        var snapshot = MatchState.Capture();
        if (snapshot.Objective is not { } crystalPosition)
        {
            WarnMissingObjectiveOnce();
            BrainTelemetry.RecordStatus(snapshot, MoveKind.Hold, "no objective (legacy)");
            return;
        }

        var enemyOnPoint = false;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (Vector3.Distance(enemy.Position, crystalPosition) < CrystalEngageRadiusYalms)
            {
                enemyOnPoint = true;
                break;
            }
        }

        var hold = Vector3.Distance(snapshot.Self, crystalPosition) < CrystalEngageRadiusYalms && enemyOnPoint;
        BrainTelemetry.RecordStatus(snapshot, hold ? MoveKind.Hold : MoveKind.Engage, hold ? "hold (legacy)" : "to crystal (legacy)");
        if (!hold)
            movement.IssueMove(crystalPosition, crystalPosition, LegacyCrystalStopRange);
        else
            movement.Stop();
    }
}
