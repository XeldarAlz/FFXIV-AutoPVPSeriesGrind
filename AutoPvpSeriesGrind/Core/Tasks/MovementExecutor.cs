using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using ECommons.Automation;
using System.Numerics;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class MovementExecutor
{
    private const float SameDestinationDriftThreshold = 2.5f;
    private const float PursuitSameDestinationDriftThreshold = 1f;
    private const float MinStopRangeForMoveCloseTo = 0.5f;
    private const float HoldRepathSlack = 1.5f;

    // Minimum gap before re-pathing to an unchanged, not-yet-reached destination (avoids per-tick pathfind spam when stuck).
    private const int RepathCooldownMs = 2000;

    private static NavIpc Nav => NavIpc.Instance;

    private Vector3 lastMoveDestination;
    private long lastMoveAtMs;
    private Posture? lastPosture;

    public void Reset()
    {
        lastMoveDestination = default;
        lastMoveAtMs = 0;
        lastPosture = null;
    }

    public bool UpdatePosture(Posture posture)
    {
        var changed = lastPosture != posture;
        lastPosture = posture;
        return changed;
    }

    public static void EnsureSprinting()
    {
        if (MatchState.HasStatus(StatusSprint))
        {
            return;
        }

        Chat.ExecuteCommand(GameCommands.Sprint);
    }

    public void HaltPathing()
    {
        if (Nav.IsRunning()) Nav.Stop();
    }

    public void Stop()
    {
        if (!Nav.IsRunning()) return;
        Nav.Stop();
        lastMoveDestination = default;
    }

    public void Execute(in MovePlan plan)
    {
        if (plan.Sprint)
        {
            EnsureSprinting();
        }

        switch (plan.Kind)
        {
            case MoveKind.Hold:
                if (DistanceToSelf(plan.Destination) > plan.StopRange + HoldRepathSlack)
                    IssueMove(plan.Destination, plan.Fallback, plan.StopRange);
                else
                    Stop();
                break;

            case MoveKind.Engage:
            case MoveKind.Retreat:
                IssueMove(plan.Destination, plan.Fallback, plan.StopRange, plan.Pursue);
                break;
        }
    }

    public void IssueMove(Vector3 destination, Vector3 fallback, float stopRange, bool pursue = false)
    {
        var driftThreshold = pursue ? PursuitSameDestinationDriftThreshold : SameDestinationDriftThreshold;
        var stopSlack = pursue ? 0f : SameDestinationDriftThreshold;
        if (ShouldSkipRepath(destination, stopRange, driftThreshold, stopSlack))
        {
            return;
        }

        lastMoveDestination = destination;
        lastMoveAtMs = Environment.TickCount64;
        var target = Nav.NearestPointReachable(destination)
                     ?? (fallback != destination ? Nav.NearestPointReachable(fallback) : null)
                     ?? fallback;
        if (stopRange > MinStopRangeForMoveCloseTo) Nav.MoveCloseTo(target, stopRange);
        else Nav.MoveTo(target);
    }

    private bool ShouldSkipRepath(Vector3 destination, float stopRange, float driftThreshold, float stopSlack)
    {
        if (Vector3.Distance(destination, lastMoveDestination) >= driftThreshold)
        {
            return false;
        }

        if (Nav.IsRunning())
        {
            return true;
        }

        if (DistanceToSelf(destination) <= stopRange + stopSlack)
        {
            return true;
        }

        return Environment.TickCount64 - lastMoveAtMs < RepathCooldownMs;
    }

    private static float DistanceToSelf(Vector3 point)
        => MatchState.PlayerPosition() is { } self ? Vector3.Distance(self, point) : float.MaxValue;
}
