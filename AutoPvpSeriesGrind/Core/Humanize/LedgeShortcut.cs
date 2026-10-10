using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Humanize;

internal static class LedgeShortcut
{
    public const int MaxJumps = 2;

    private const float MaxStraightYalms = 18f;
    private const float MinDetourRatio = 1.5f;
    private const float MinSavedYalms = 4f;
    private const float SampleStepYalms = 0.75f;
    private const float StepUpYalms = 0.35f;
    private const float MaxJumpRiseYalms = 1f;
    private const float MaxDropYalms = 3.5f;
    private const float EndHeightToleranceYalms = 1.2f;
    private const float ChestHeightYalms = 1.6f;
    private const int MaxCandidates = 3;

    public static bool TryPlan(Vector3 self, List<Vector3> waypoints, Func<Vector3, bool> rejected, Span<Vector3> edges,
        out int edgeCount, out int skipCount)
    {
        edgeCount = 0;
        skipCount = 0;
        Span<int> candidates = stackalloc int[MaxCandidates];
        var candidateCount = CollectCandidates(self, waypoints, candidates);
        for (var candidateIndex = candidateCount - 1; candidateIndex >= 0; candidateIndex--)
        {
            var endIndex = candidates[candidateIndex];
            var end = waypoints[endIndex];
            if (rejected(end) || !TryProfile(self, end, edges, out edgeCount))
            {
                continue;
            }

            skipCount = endIndex;
            return true;
        }

        edgeCount = 0;
        return false;
    }

    private static int CollectCandidates(Vector3 self, List<Vector3> waypoints, Span<int> candidates)
    {
        var candidateCount = 0;
        var walked = 0f;
        var previous = self;
        for (var waypointIndex = 0; waypointIndex < waypoints.Count; waypointIndex++)
        {
            var waypoint = waypoints[waypointIndex];
            walked += VectorMath.HorizontalDistance(previous, waypoint);
            previous = waypoint;
            var straight = VectorMath.HorizontalDistance(self, waypoint);
            if (straight > MaxStraightYalms)
            {
                break;
            }
            if (waypointIndex == 0 || walked < straight * MinDetourRatio || walked - straight < MinSavedYalms)
            {
                continue;
            }

            if (candidateCount == candidates.Length)
            {
                candidates[1..].CopyTo(candidates);
                candidateCount--;
            }
            candidates[candidateCount++] = waypointIndex;
        }
        return candidateCount;
    }

    private static bool TryProfile(Vector3 self, Vector3 end, Span<Vector3> edges, out int edgeCount)
    {
        edgeCount = 0;
        var length = VectorMath.HorizontalDistance(self, end);
        var steps = Math.Max(1, (int)MathF.Ceiling(length / SampleStepYalms));
        var probeFromY = MathF.Max(self.Y, end.Y);
        var previousGround = self.Y;
        var previousPoint = self;
        for (var stepIndex = 1; stepIndex <= steps; stepIndex++)
        {
            var point = Vector3.Lerp(self, end, (float)stepIndex / steps);
            var chestFrom = previousPoint with { Y = previousGround + ChestHeightYalms };
            var chestTo = point with { Y = previousGround + ChestHeightYalms };
            if (!Terrain.TryBlocked(chestFrom, chestTo, out var blocked) || blocked)
            {
                return false;
            }
            if (Terrain.GroundBelow(point, probeFromY) is not { } ground)
            {
                return false;
            }

            var rise = ground - previousGround;
            if (rise > MaxJumpRiseYalms || rise < -MaxDropYalms)
            {
                return false;
            }
            if (rise > StepUpYalms)
            {
                if (edgeCount == edges.Length)
                {
                    return false;
                }
                edges[edgeCount++] = Vector3.Lerp(previousPoint, point, 0.5f) with { Y = previousGround };
            }

            previousGround = ground;
            previousPoint = point;
        }
        return MathF.Abs(previousGround - end.Y) <= EndHeightToleranceYalms;
    }
}
