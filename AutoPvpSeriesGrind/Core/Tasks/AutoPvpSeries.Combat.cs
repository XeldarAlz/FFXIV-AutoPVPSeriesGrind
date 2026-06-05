using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Util;
using ECommons.DalamudServices;
using System.Numerics;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    // Radius (yalms) around the crystal used for "on point" engagement checks.
    private const float CrystalEngageRadius = 10f;
    private const float LegacyCrystalStopRange = 1.5f;

    private async Task TickLiveMatch()
    {
        rotation.TickDeathAndRespawn();
        if (IsDead())
        {
            rotation.OnDeadDuringLive();
            movement.Stop();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Retreat, "dead — waiting to respawn", Posture.Retreat);
            return;
        }

        rotation.EnsureSignCleared();
        rotation.EnsureRotationEnabled();

        var territory = Svc.ClientState.TerritoryType;

        if (MatchState.HasStatus(StatusSpawnProtection))
        {
            if (!MatchState.HasStatus(StatusSprint))
                Cmd(GameCommands.Sprint);
            ranSafetyMoveThisDuty = true;
        }

        if (MatchState.LocalIsCasting(ActionStandardIssueElixir))
        {
            movement.Stop();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Hold, "wait: elixir cast (hp/mp refill)", Posture.Hold);
            return;
        }

        if (settings.EnableBrain)
            await RunBrainTick(territory);
        else
            LegacyCrystalMove();
    }

    private async Task RunBrainTick(uint territory)
    {
        var snap = MatchState.Capture();

        if (!snap.HasObjective)
        {
            BrainTelemetry.Record(snap, new MovePlan(MoveKind.Hold, snap.Self, snap.Self, 0f, false, "no objective"));
            movement.HaltPathing();
            return;
        }

        var anchor = MatchState.NearestSafeAnchor(territory, snap.Self) ?? snap.Self;
        var plan = brain.Decide(snap, anchor);
        BrainTelemetry.Record(snap, plan);

        var planChanged = movement.UpdatePosture(plan.Posture);
        if (settings.Humanize != HumanizeLevel.Off && planChanged)
        {
            var (min, max) = HumanTiming.ReactionBand(settings.Humanize);
            await NextFrame(HumanTiming.Reaction(min, max));
        }

        movement.Execute(plan);
    }

    private void LegacyCrystalMove()
    {
        var snap = MatchState.Capture();
        if (snap.Objective is not { } c)
        {
            BrainTelemetry.RecordStatus(snap, MoveKind.Hold, "no objective (legacy)");
            return;
        }

        var enemyOnPoint = snap.Enemies.Any(e => Vector3.Distance(e.Position, c) < CrystalEngageRadius);
        var hold = Vector3.Distance(snap.Self, c) < CrystalEngageRadius && enemyOnPoint;
        BrainTelemetry.RecordStatus(snap, hold ? MoveKind.Hold : MoveKind.Engage, hold ? "hold (legacy)" : "to crystal (legacy)");
        if (!hold)
            movement.IssueMove(c, c, LegacyCrystalStopRange);
        else
            movement.Stop();
    }
}
