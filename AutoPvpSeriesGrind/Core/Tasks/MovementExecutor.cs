using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Humanize;
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
    private const int StuckStrikeWindowMs = 12000;
    private const float StuckSameDestinationYalms = 4f;
    private const int StuckStrikesBeforeGivingUp = 3;

    private const int PathfindTimeoutMs = 5000;
    private const float MaxTravelYalmsPerSecond = 20f;
    private const float PassedWaypointSlackYalms = 3f;

    private const float PartialPathSlackYalms = 3f;
    private const float OffLevelSnapYalms = 3f;
    private const float ApproachArrivalYalms = 3f;
    private const float UnreachableSpotYalms = 4f;
    private const int UnreachableRememberMs = 30000;
    private const float FailedShortcutSpotYalms = 3f;
    private const int FailedShortcutRememberMs = 120000;
    private const float ViaJoinSlackYalms = 2f;

    private const float RouteSnapExtentYalms = 3f;
    private const float CornerSnapExtentXZ = 0.5f;
    private const float CornerSnapExtentY = 1.5f;

    private static NavIpc Nav => NavIpc.Instance;

    public bool IsPathing => Nav.IsFollowingPath() || HasLivePendingPath;

    private readonly StuckDetector stuck = new();
    private readonly SpotMemory unreachable = new(UnreachableSpotYalms, UnreachableRememberMs);
    private readonly SpotMemory failedShortcuts = new(FailedShortcutSpotYalms, FailedShortcutRememberMs);
    private readonly RoutePlanner route = new();
    private readonly JumpScheduler jumps = new();
    private readonly Func<Vector3, Vector3?> snapRoute;
    private readonly Func<Vector3, Vector3?> snapCorner;
    private readonly Func<Vector3, bool> shortcutRejected;

    private Persona persona = Persona.Neutral;
    private IReadOnlyList<PvpStructure> obstacles = [];

    private Vector3 lastMoveDestination;
    private long lastMoveAtMs;
    private long destinationCommittedAtMs;
    private int jitterSign = 1;
    private Posture? lastPosture;
    private Vector3 lastStuckDestination;
    private long lastStuckAtMs;
    private int stuckStrikes;

    private Task<List<Vector3>>? pendingPath;
    private Task<List<Vector3>>? pendingTail;
    private Vector3? pendingVia;
    private Vector3 pendingDestination;
    private Vector3 pendingTarget;
    private float pendingStopRange;
    private long pendingSinceMs;
    private bool pendingDiscarded;
    private long navmeshWaitStartedAtMs;

    public MovementExecutor()
    {
        snapRoute = static point => Nav.NearestPointReachable(point, RouteSnapExtentYalms, RouteSnapExtentYalms);
        snapCorner = static point => Nav.NearestPointReachable(point, CornerSnapExtentXZ, CornerSnapExtentY);
        shortcutRejected = point => failedShortcuts.Contains(point, Environment.TickCount64);
    }

    private bool HasLivePendingPath => pendingPath is not null && !pendingDiscarded;

    public void SetPersona(in Persona value) => persona = value;

    public void SetObstacles(IReadOnlyList<PvpStructure> structures) => obstacles = structures;

    public bool IsReachable(Vector3 point) => !unreachable.Contains(point, Environment.TickCount64);

    public void Reset()
    {
        stuck.Reset();
        unreachable.Clear();
        failedShortcuts.Clear();
        route.Reset();
        jumps.Reset();
        obstacles = [];
        lastMoveDestination = default;
        lastMoveAtMs = 0;
        destinationCommittedAtMs = 0;
        lastPosture = null;
        stuckStrikes = 0;
        lastStuckAtMs = 0;
        pendingPath = null;
        pendingTail = null;
        pendingDiscarded = false;
        navmeshWaitStartedAtMs = 0;
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
        jumps.ClearShortcut();
        if (Nav.IsFollowingPath())
        {
            Nav.Stop();
        }
    }

    public void Stop()
    {
        jumps.ClearShortcut();
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
        Pump();
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
                MoveTowards(plan.Destination, plan.Fallback, plan.StopRange, plan.Pursue);
                break;
        }
    }

    public void IssueMove(Vector3 destination, Vector3 fallback, float stopRange, bool pursue = false)
    {
        Pump();
        MoveTowards(destination, fallback, stopRange, pursue);
    }

    private void MoveTowards(Vector3 destination, Vector3 fallback, float stopRange, bool pursue)
    {
        if (!NavmeshReady())
        {
            return;
        }

        var now = Environment.TickCount64;
        if (unreachable.TryRecall(destination, now, out var approach))
        {
            if (DistanceToSelf(approach) <= stopRange + ApproachArrivalYalms)
            {
                return;
            }
            destination = approach;
            fallback = approach;
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

        var target = ResolveTarget(destination, fallback, rememberOffLevel: !pursue, now);
        if (!RequestRoute(self, target, now))
        {
            return;
        }

        pendingDestination = destination;
        pendingTarget = target;
        pendingStopRange = stopRange;
        pendingSinceMs = now;
        pendingDiscarded = false;

        lastMoveDestination = destination;
        lastMoveAtMs = now;
        destinationCommittedAtMs = now;
    }

    private Vector3 ResolveTarget(Vector3 destination, Vector3 fallback, bool rememberOffLevel, long now)
    {
        if (Nav.NearestPointReachable(destination) is not { } snapped)
        {
            return (fallback != destination ? Nav.NearestPointReachable(fallback) : null) ?? fallback;
        }

        if (rememberOffLevel && MathF.Abs(snapped.Y - destination.Y) > OffLevelSnapYalms)
        {
            unreachable.Remember(destination, snapped, now);
            RunLog.Debug($"{destination:F0} sits on a level the mesh cannot reach -> heading for {snapped:F0} instead");
        }
        return snapped;
    }

    private bool RequestRoute(Vector3 self, Vector3 target, long now)
    {
        var via = route.Via(self, target, in persona, snapRoute, out var newTrip);
        if (newTrip)
        {
            jumps.PlanHop(VectorMath.HorizontalDistance(self, target), persona.HopChance, now);
        }

        if (via is { } viaPoint
            && Nav.Pathfind(self, viaPoint) is { } head
            && Nav.Pathfind(viaPoint, target) is { } tail)
        {
            pendingPath = head;
            pendingTail = tail;
            pendingVia = viaPoint;
            return true;
        }

        if (via is not null)
        {
            route.Abandon();
        }
        if (Nav.Pathfind(self, target) is not { } direct)
        {
            return false;
        }

        pendingPath = direct;
        pendingTail = null;
        pendingVia = null;
        return true;
    }

    private void HoldAt(Vector3 destination, Vector3 fallback, float stopRange)
    {
        var holdRadius = stopRange + HoldRepathSlack;
        if (DistanceToSelf(destination) > holdRadius)
        {
            MoveTowards(destination, fallback, stopRange, pursue: false);
            return;
        }
        if (Vector3.Distance(lastMoveDestination, destination) > holdRadius)
        {
            Stop();
        }
    }

    private void Pump()
    {
        ApplyFinishedPath();
        if (MatchState.PlayerPosition() is not { } self)
        {
            return;
        }

        var now = Environment.TickCount64;
        jumps.Tick(self, Nav.IsFollowingPath(), VectorMath.HorizontalDistance(self, lastMoveDestination), now);
        if (jumps.ShortcutStalled)
        {
            AbandonShortcut(now);
        }
    }

    private void AbandonShortcut(long now)
    {
        failedShortcuts.Remember(jumps.ShortcutEnd, jumps.ShortcutEnd, now);
        RunLog.Debug($"ledge shortcut to {jumps.ShortcutEnd:F0} is blocked -> back to the mesh route");
        Stop();
        lastMoveAtMs = 0;
    }

    private void ApplyFinishedPath()
    {
        if (pendingPath is not { } head)
        {
            return;
        }

        var ageMs = Environment.TickCount64 - pendingSinceMs;
        if (!head.IsCompleted || pendingTail is { IsCompleted: false })
        {
            if (ageMs > PathfindTimeoutMs)
            {
                RunLog.Debug($"pathfind to {pendingDestination:F0} still running after {ageMs}ms, giving up on it");
                ClearPending();
            }
            return;
        }

        var tail = pendingTail;
        var discarded = pendingDiscarded;
        ClearPending();
        if (discarded)
        {
            return;
        }
        if (!head.IsCompletedSuccessfully || tail is { IsCompletedSuccessfully: false })
        {
            RunLog.Debug($"pathfind to {pendingDestination:F0} failed: {(head.Exception ?? tail?.Exception)?.GetBaseException().Message}");
            return;
        }
        if (MatchState.PlayerPosition() is not { } self || Assemble(self, head.Result, tail?.Result) is not { } waypoints)
        {
            return;
        }

        Shape(self, waypoints);
        PathTrim.ShortenTail(waypoints, pendingStopRange);
        PathTrim.DropPassed(waypoints, self, ageMs / 1000f * MaxTravelYalmsPerSecond + PassedWaypointSlackYalms);
        if (waypoints.Count == 0)
        {
            return;
        }

        if (persona.JumpsLedges && !Svc.Condition[ConditionFlag.Mounted])
        {
            jumps.TryShortcut(self, waypoints, shortcutRejected, Environment.TickCount64);
        }
        Nav.FollowWaypoints(waypoints);
    }

    private List<Vector3>? Assemble(Vector3 self, List<Vector3> head, List<Vector3>? tail)
    {
        if (head.Count == 0)
        {
            return null;
        }
        if (tail is null || pendingVia is not { } via)
        {
            return head;
        }

        if (Vector3.Distance(head[^1], via) > ViaJoinSlackYalms || tail.Count == 0)
        {
            RunLog.Debug($"detour point {via:F0} is out of reach -> taking the direct route");
            route.Abandon();
            lastMoveAtMs = 0;
            return null;
        }

        head.AddRange(tail);
        if (RoutePlanner.IsDetourTooLong(self, pendingTarget, VectorMath.PathLength(self, head, head.Count - 1)))
        {
            RunLog.Debug($"detour through {via:F0} runs too long -> taking the direct route");
            route.Abandon();
            lastMoveAtMs = 0;
            return null;
        }
        return head;
    }

    private void Shape(Vector3 self, List<Vector3> waypoints)
    {
        var end = waypoints[^1];
        if (Vector3.Distance(end, pendingTarget) > PartialPathSlackYalms)
        {
            unreachable.Remember(pendingDestination, end, Environment.TickCount64);
            RunLog.Debug($"no full path to {pendingDestination:F0} (ends {Vector3.Distance(end, pendingTarget):F1}y short) -> going as close as the mesh allows");
        }

        RouteShaper.SkirtStructures(waypoints, obstacles, snapRoute);
        RouteShaper.RoundCorners(waypoints, persona.CornerRounding, snapCorner);
    }

    private void ClearPending()
    {
        pendingPath = null;
        pendingTail = null;
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

        var now = Environment.TickCount64;
        stuck.Reset();
        if (jumps.ShortcutActive)
        {
            AbandonShortcut(now);
            return true;
        }

        var strikes = CountStuckStrike(plan.Destination, now);
        if (strikes == 1 && JumpOps.TryJump())
        {
            RunLog.Debug("stuck on a snag -> jumping it");
            return true;
        }
        if (strikes >= StuckStrikesBeforeGivingUp)
        {
            unreachable.Remember(plan.Destination, self, now);
            RunLog.Debug($"stuck {strikes} times heading for {plan.Destination:F0} -> leaving it alone for a while");
            Stop();
            return true;
        }

        Stop();
        lastMoveAtMs = 0;
        MoveTowards(JitterPerpendicular(plan.Destination, self), plan.Fallback, plan.StopRange, plan.Pursue);
        return true;
    }

    private int CountStuckStrike(Vector3 destination, long now)
    {
        var repeated = now - lastStuckAtMs < StuckStrikeWindowMs
                       && Vector3.Distance(destination, lastStuckDestination) <= StuckSameDestinationYalms;
        stuckStrikes = repeated ? stuckStrikes + 1 : 1;
        lastStuckDestination = destination;
        lastStuckAtMs = now;
        return stuckStrikes;
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
