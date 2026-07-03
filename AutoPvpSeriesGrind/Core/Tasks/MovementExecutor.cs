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
    private const float MinJitterDirectionSq = 0.01f;

    // Minimum gap before re-pathing to an unchanged, not-yet-reached destination (avoids per-tick pathfind spam when stuck).
    private const int RepathCooldownMs = 2000;

    private const int DestinationCommitMs = 1800;
    private const float CommittedDriftThreshold = 6f;

    private const float StuckJitterYalms = 3f;

    private static NavIpc Nav => NavIpc.Instance;

    private readonly StuckDetector stuck = new();

    private Vector3 lastMoveDestination;
    private long lastMoveAtMs;
    private long destinationCommittedAtMs;
    private int jitterSign = 1;
    private Posture? lastPosture;

    public void Reset()
    {
        stuck.Reset();
        lastMoveDestination = default;
        lastMoveAtMs = 0;
        destinationCommittedAtMs = 0;
        lastPosture = null;
    }

    public bool UpdatePosture(Posture posture)
    {
        var changed = lastPosture != posture;
        lastPosture = posture;
        if (changed)
        {
            destinationCommittedAtMs = 0;
        }
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

    public void Stop(bool? running = null)
    {
        if (!(running ?? Nav.IsRunning()))
        {
            return;
        }
        Nav.Stop();
        lastMoveDestination = default;
        destinationCommittedAtMs = 0;
    }

    public void Execute(in MovePlan plan)
    {
        if (plan.Sprint)
        {
            EnsureSprinting();
        }

        var running = Nav.IsRunning();
        if (TryRecoverFromStuck(plan, running))
        {
            return;
        }

        switch (plan.Kind)
        {
            case MoveKind.Hold:
                if (DistanceToSelf(plan.Destination) > plan.StopRange + HoldRepathSlack)
                {
                    IssueMove(plan.Destination, plan.Fallback, plan.StopRange, running: running);
                }
                else
                {
                    Stop(running);
                }
                break;

            case MoveKind.Engage:
            case MoveKind.Retreat:
                IssueMove(plan.Destination, plan.Fallback, plan.StopRange, plan.Pursue, running);
                break;
        }
    }

    public void IssueMove(Vector3 destination, Vector3 fallback, float stopRange, bool pursue = false, bool? running = null)
    {
        var isRunning = running ?? Nav.IsRunning();
        var driftThreshold = DriftThreshold(pursue);
        var stopSlack = pursue ? 0f : SameDestinationDriftThreshold;
        if (ShouldSkipRepath(destination, stopRange, driftThreshold, stopSlack, isRunning))
        {
            return;
        }

        lastMoveDestination = destination;
        lastMoveAtMs = Environment.TickCount64;
        destinationCommittedAtMs = lastMoveAtMs;
        var target = Nav.NearestPointReachable(destination)
                     ?? (fallback != destination ? Nav.NearestPointReachable(fallback) : null)
                     ?? fallback;
        if (stopRange > MinStopRangeForMoveCloseTo) Nav.MoveCloseTo(target, stopRange);
        else Nav.MoveTo(target);
    }

    private bool TryRecoverFromStuck(in MovePlan plan, bool running)
    {
        if (MatchState.PlayerPosition() is not { } self)
        {
            return false;
        }
        if (!stuck.IsStuck(self, running))
        {
            return false;
        }

        Nav.Stop();
        lastMoveDestination = default;
        lastMoveAtMs = 0;
        destinationCommittedAtMs = 0;
        IssueMove(JitterPerpendicular(plan.Destination, self), plan.Fallback, plan.StopRange, plan.Pursue, running: false);
        return true;
    }

    private Vector3 JitterPerpendicular(Vector3 destination, Vector3 self)
    {
        var direction = destination - self;
        direction.Y = 0;
        if (direction.LengthSquared() <= MinJitterDirectionSq)
        {
            return destination;
        }
        var perpendicular = Vector3.Normalize(new Vector3(-direction.Z, 0, direction.X));
        jitterSign = -jitterSign;
        return destination + perpendicular * (StuckJitterYalms * jitterSign);
    }

    private float DriftThreshold(bool pursue)
    {
        if (pursue)
        {
            return PursuitSameDestinationDriftThreshold;
        }
        return WithinCommitWindow() ? CommittedDriftThreshold : SameDestinationDriftThreshold;
    }

    private bool WithinCommitWindow()
        => destinationCommittedAtMs != 0 && Environment.TickCount64 - destinationCommittedAtMs < DestinationCommitMs;

    private bool ShouldSkipRepath(Vector3 destination, float stopRange, float driftThreshold, float stopSlack, bool running)
    {
        if (Vector3.Distance(destination, lastMoveDestination) >= driftThreshold)
        {
            return false;
        }

        if (running)
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
