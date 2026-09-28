using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Rotation;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class MovementExecutor
{
    private const float SameDestinationDriftThreshold = 2.5f;
    private const float PursuitSameDestinationDriftThreshold = 1f;
    private const float HoldRepathSlack = 1.5f;
    private const float MinJitterDirectionSq = 0.01f;

    private const int RepathCooldownMs = 2000;

    private const int DestinationCommitMs = 1800;
    private const float CommittedDriftThreshold = 6f;

    private const float StuckJitterYalms = 3f;

    private const int PathfindTimeoutMs = 5000;
    private const float MaxTravelYalmsPerSecond = 20f;
    private const float PassedWaypointSlackYalms = 3f;

    private static NavIpc Nav => NavIpc.Instance;

    public bool IsPathing => Nav.IsFollowingPath() || HasLivePendingPath;

    private readonly StuckDetector stuck = new();

    private Vector3 lastMoveDestination;
    private long lastMoveAtMs;
    private long destinationCommittedAtMs;
    private int jitterSign = 1;
    private Posture? lastPosture;

    private Task<List<Vector3>>? pendingPath;
    private Vector3 pendingDestination;
    private float pendingStopRange;
    private long pendingSinceMs;
    private bool pendingDiscarded;
    private long navmeshWaitStartedAtMs;

    private bool HasLivePendingPath => pendingPath is not null && !pendingDiscarded;

    public void Reset()
    {
        stuck.Reset();
        lastMoveDestination = default;
        lastMoveAtMs = 0;
        destinationCommittedAtMs = 0;
        lastPosture = null;
        pendingPath = null;
        pendingDiscarded = false;
        navmeshWaitStartedAtMs = 0;
        WalkPace.Release();
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
        if (Svc.Condition[ConditionFlag.Mounted] || MatchState.HasStatus(PvpStatuses.Sprint) || MatchState.HasStatus(PvpStatuses.Guard))
        {
            return;
        }

        ActionOps.UseAction(PvpActions.Sprint);
    }

    public void HaltPathing()
    {
        DiscardPendingPath();
        if (Nav.IsFollowingPath())
        {
            Nav.Stop();
        }
    }

    public void Stop()
    {
        WalkPace.Release();
        var discarded = DiscardPendingPath();
        if (!discarded && !Nav.IsFollowingPath())
        {
            return;
        }

        Nav.Stop();
        lastMoveDestination = default;
        destinationCommittedAtMs = 0;
    }

    public void Execute(in MovePlan plan)
    {
        ApplyFinishedPath();
        WalkPace.Apply(plan.Walk);
        if (plan.Sprint)
        {
            EnsureSprinting();
        }

        if (TryRecoverFromStuck(plan))
        {
            return;
        }

        switch (plan.Kind)
        {
            case MoveKind.Hold:
                HoldAt(plan.Destination, plan.Fallback, plan.StopRange);
                break;

            case MoveKind.Engage:
            case MoveKind.Retreat:
                IssueMove(plan.Destination, plan.Fallback, plan.StopRange, plan.Pursue);
                break;
        }
    }

    public void IssueMove(Vector3 destination, Vector3 fallback, float stopRange, bool pursue = false)
    {
        ApplyFinishedPath();
        if (!NavmeshReady())
        {
            return;
        }

        var driftThreshold = DriftThreshold(pursue);
        var stopSlack = pursue ? 0f : SameDestinationDriftThreshold;
        if (ShouldSkipRepath(destination, stopRange, driftThreshold, stopSlack))
        {
            return;
        }
        if (pendingPath is not null || MatchState.PlayerPosition() is not { } self)
        {
            return;
        }

        var target = Nav.NearestPointReachable(destination)
                     ?? (fallback != destination ? Nav.NearestPointReachable(fallback) : null)
                     ?? fallback;
        if (Nav.Pathfind(self, target) is not { } request)
        {
            return;
        }

        pendingPath = request;
        pendingDestination = destination;
        pendingStopRange = stopRange;
        pendingSinceMs = Environment.TickCount64;
        pendingDiscarded = false;

        lastMoveDestination = destination;
        lastMoveAtMs = pendingSinceMs;
        destinationCommittedAtMs = pendingSinceMs;
    }

    private void HoldAt(Vector3 destination, Vector3 fallback, float stopRange)
    {
        var holdRadius = stopRange + HoldRepathSlack;
        if (DistanceToSelf(destination) > holdRadius)
        {
            IssueMove(destination, fallback, stopRange);
            return;
        }
        if (Vector3.Distance(lastMoveDestination, destination) > holdRadius)
        {
            Stop();
        }
    }

    private void ApplyFinishedPath()
    {
        if (pendingPath is not { } request)
        {
            return;
        }

        var ageMs = Environment.TickCount64 - pendingSinceMs;
        if (!request.IsCompleted)
        {
            if (ageMs > PathfindTimeoutMs)
            {
                RunLog.Debug($"pathfind to {pendingDestination:F0} still running after {ageMs}ms, giving up on it");
                pendingPath = null;
            }
            return;
        }

        pendingPath = null;
        if (pendingDiscarded)
        {
            return;
        }
        if (!request.IsCompletedSuccessfully)
        {
            RunLog.Debug($"pathfind to {pendingDestination:F0} failed: {request.Exception?.GetBaseException().Message}");
            return;
        }

        var waypoints = request.Result;
        PathTrim.ShortenTail(waypoints, pendingStopRange);
        if (MatchState.PlayerPosition() is { } self)
        {
            PathTrim.DropPassed(waypoints, self, ageMs / 1000f * MaxTravelYalmsPerSecond + PassedWaypointSlackYalms);
        }
        if (waypoints.Count == 0)
        {
            return;
        }

        Nav.FollowWaypoints(waypoints);
    }

    private bool DiscardPendingPath()
    {
        if (!HasLivePendingPath)
        {
            return false;
        }

        pendingDiscarded = true;
        return true;
    }

    private bool NavmeshReady()
    {
        if (Nav.IsReady())
        {
            if (navmeshWaitStartedAtMs != 0)
            {
                RunLog.Info($"navmesh ready after {(Environment.TickCount64 - navmeshWaitStartedAtMs) / 1000f:F1}s -> moving");
                navmeshWaitStartedAtMs = 0;
            }
            return true;
        }

        if (navmeshWaitStartedAtMs == 0)
        {
            navmeshWaitStartedAtMs = Environment.TickCount64;
            RunLog.Info($"navmesh still loading (progress {Nav.BuildProgress():F2}) -> holding movement until it is ready");
        }
        return false;
    }

    private bool TryRecoverFromStuck(in MovePlan plan)
    {
        if (MatchState.PlayerPosition() is not { } self)
        {
            return false;
        }
        if (!stuck.IsStuck(self, Nav.IsFollowingPath()))
        {
            return false;
        }

        Stop();
        stuck.Reset();
        lastMoveAtMs = 0;
        IssueMove(JitterPerpendicular(plan.Destination, self), plan.Fallback, plan.StopRange, plan.Pursue);
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

    private bool ShouldSkipRepath(Vector3 destination, float stopRange, float driftThreshold, float stopSlack)
    {
        if (Vector3.Distance(destination, lastMoveDestination) >= driftThreshold)
        {
            return false;
        }

        if (IsPathing)
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
