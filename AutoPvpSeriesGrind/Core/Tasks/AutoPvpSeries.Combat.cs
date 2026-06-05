using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
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

    private const float RepathThreshold = 2.5f;     // skip re-issuing a path if the new dest is this close to the last
    private const float PursuitRepathThreshold = 1f;
    private const float MinStopRange = 0.5f;        // below this we MoveTo exactly instead of MoveCloseTo
    private const float HoldSlack = 1.5f;           // extra slack before we bother re-pathing while holding
    private const float LegacyCrystalStopRange = 1.5f;

    // Minimum gap before re-pathing to an unchanged, not-yet-reached destination (avoids per-tick pathfind spam when stuck).
    private const int RepathCooldownMs = 2000;

    private static NavIpc Nav => NavIpc.Instance;

    private async Task TickLiveMatch()
    {
        CheckDeathAndReapplyRotation();
        if (IsDead())
        {
            hasEnabledRotationThisLife = false;
            StopMoving();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Retreat, "dead — waiting to respawn");
            return;
        }

        if (!clearedSignThisLife)
        {
            Cmd(GameCommands.ClearEnemySignOnSelf);
            clearedSignThisLife = true;
        }

        if (IsNormal() && !hasEnabledRotationThisLife)
        {
            Cmd(GameCommands.EnableRotation);
            hasEnabledRotationThisLife = true;
            Diag("rotation enabled (live failsafe)");
        }

        var territory = Svc.ClientState.TerritoryType;

        if (MatchState.HasStatus(StatusSpawnProtection))
        {
            if (!MatchState.HasStatus(StatusSprint))
                Cmd(GameCommands.Sprint);
            ranSafetyMoveThisDuty = true;
        }

        if (MatchState.LocalIsCasting(ActionStandardIssueElixir))
        {
            StopMoving();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Hold, "wait: elixir cast (hp/mp refill)");
            return;
        }

        if (enableBrain)
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
            if (Nav.IsRunning()) Nav.Stop();
            return;
        }

        var anchor = MatchState.NearestSafeAnchor(territory, snap.Self) ?? snap.Self;
        var plan = brain.Decide(snap, anchor);
        BrainTelemetry.Record(snap, plan);

        if (humanize != HumanizeLevel.Off && lastPlanKind != plan.Kind)
        {
            var (min, max) = HumanTiming.ReactionBand(humanize);
            await NextFrame(HumanTiming.Reaction(min, max));
        }
        lastPlanKind = plan.Kind;

        ExecutePlan(plan);
    }

    private void ExecutePlan(in MovePlan plan)
    {
        if (plan.Sprint && !MatchState.HasStatus(StatusSprint))
            Cmd(GameCommands.Sprint);

        switch (plan.Kind)
        {
            case MoveKind.Hold:
                if (DistanceToSelf(plan.Destination) > plan.StopRange + HoldSlack)
                    IssueMove(plan.Destination, plan.Fallback, plan.StopRange);
                else
                    StopMoving();
                break;

            case MoveKind.Engage:
            case MoveKind.Retreat:
                IssueMove(plan.Destination, plan.Fallback, plan.StopRange, plan.Pursue);
                break;
        }
    }

    private void IssueMove(Vector3 dest, Vector3 fallback, float stopRange, bool pursue = false)
    {
        var drift = Vector3.Distance(dest, lastMoveDest);

        if (pursue)
        {
            if (drift < PursuitRepathThreshold)
            {
                if (Nav.IsRunning()) return;
                if (DistanceToSelf(dest) <= stopRange) return;
                if (Environment.TickCount64 - lastMoveAtMs < RepathCooldownMs) return;
            }
        }
        else if (drift < RepathThreshold)
        {
            if (Nav.IsRunning()) return;
            if (DistanceToSelf(dest) <= stopRange + RepathThreshold) return;
            if (Environment.TickCount64 - lastMoveAtMs < RepathCooldownMs) return;
        }

        lastMoveDest = dest;
        lastMoveAtMs = Environment.TickCount64;
        var target = Nav.NearestPointReachable(dest)
                     ?? (fallback != dest ? Nav.NearestPointReachable(fallback) : null)
                     ?? fallback;
        if (stopRange > MinStopRange) Nav.MoveCloseTo(target, stopRange);
        else Nav.MoveTo(target);
    }

    private void StopMoving()
    {
        if (!Nav.IsRunning()) return;
        Nav.Stop();
        lastMoveDest = default;
    }

    private static float DistanceToSelf(Vector3 p)
        => MatchState.PlayerPosition() is { } self ? Vector3.Distance(self, p) : float.MaxValue;

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
            IssueMove(c, c, LegacyCrystalStopRange);
        else
            StopMoving();
    }
}
