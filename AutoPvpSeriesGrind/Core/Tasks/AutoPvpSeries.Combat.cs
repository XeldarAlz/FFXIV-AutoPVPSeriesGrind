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
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Retreat, "dead — waiting to respawn", Posture.Retreat);
            return;
        }

        rotation.EnsureSignCleared();
        rotation.EnsureRotationEnabled();

        var territory = Svc.ClientState.TerritoryType;

        if (MatchState.HasStatus(StatusSpawnProtection))
        {
            MovementExecutor.EnsureSprinting();
            matchFlow.RanSafetyMoveThisDuty = true;
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

    private bool TryLeaveSpawn(uint territory)
    {
        if (MatchState.PlayerPosition() is not { } self)
            return true;

        if (MatchState.NearestSafeAnchor(territory, self) is not { } exit)
        {
            matchFlow.LeftSpawn = true;
            return true;
        }

        if (matchFlow.LeaveSpawnStartedAtMs == 0)
        {
            matchFlow.LeaveSpawnStartedAtMs = Environment.TickCount64;
        }

        var flat = VectorMath.HorizontalDistance(self, exit);
        var timedOut = Environment.TickCount64 - matchFlow.LeaveSpawnStartedAtMs > SpawnExitTimeoutMs;
        if (flat <= SpawnExitArrivalRange || timedOut)
        {
            matchFlow.LeftSpawn = true;
            LogDiagnostic(timedOut
                ? $"leave-spawn timeout ({SpawnExitTimeoutMs}ms, {flat:F1}y out) -> handing off to brain"
                : $"off the spawn platform (anchor {flat:F1}y) -> brain takes over");
            return true;
        }

        movement.IssueMove(exit, exit, SpawnExitArrivalRange);
        BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Engage, "leaving spawn — to gate anchor", Posture.Reposition);
        return false;
    }

    private async Task RunBrainTick(uint territory)
    {
        var snapshot = MatchState.Capture();

        if (!snapshot.HasObjective)
        {
            BrainTelemetry.Record(snapshot, new MovePlan(MoveKind.Hold, snapshot.Self, snapshot.Self, 0f, false, "no objective"));
            movement.HaltPathing();
            return;
        }

        var anchor = MatchState.NearestSafeAnchor(territory, snapshot.Self) ?? snapshot.Self;
        var plan = brain.Decide(snapshot, anchor);
        BrainTelemetry.Record(snapshot, plan);

        var planChanged = movement.UpdatePosture(plan.Posture);
        if (settings.Humanize != HumanizeLevel.Off && planChanged)
        {
            var (reactionMinMs, reactionMaxMs) = HumanTiming.ReactionBand(settings.Humanize);
            await NextFrame(HumanTiming.Reaction(reactionMinMs, reactionMaxMs));
        }

        movement.Execute(plan);
    }

    private void LegacyCrystalMove()
    {
        var snapshot = MatchState.Capture();
        if (snapshot.Objective is not { } crystalPosition)
        {
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
