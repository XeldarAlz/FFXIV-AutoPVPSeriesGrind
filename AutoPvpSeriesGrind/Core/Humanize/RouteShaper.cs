using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Humanize;

internal sealed class RoutePlanner
{
    private const float MinTripYalms = 28f;
    private const float NewTripYalms = 12f;
    private const float ViaReachedYalms = 5f;
    private const float MinAlong = 0.35f;
    private const float MaxAlong = 0.65f;
    private const float MinLateralYalms = 4f;
    private const float MaxLateralYalms = 12f;
    private const float MaxLateralShare = 0.3f;
    private const double BaseViaChance = 0.3;
    private const double WanderViaChance = 0.6;
    private const float MaxDetourRatio = 1.6f;
    private const float DetourAllowanceYalms = 10f;

    private Vector3? tripDestination;
    private Vector3? via;

    public void Reset()
    {
        tripDestination = null;
        via = null;
    }

    public void Abandon() => via = null;

    public Vector3? Via(Vector3 self, Vector3 destination, in Persona persona, Func<Vector3, Vector3?> snap, out bool newTrip)
    {
        newTrip = tripDestination is not { } trip || Vector3.Distance(trip, destination) > NewTripYalms;
        if (newTrip)
        {
            tripDestination = destination;
            via ??= Pick(self, destination, in persona, snap);
        }

        if (via is { } point
            && (VectorMath.HorizontalDistance(self, point) <= ViaReachedYalms
                || VectorMath.HorizontalDistance(self, destination) <= VectorMath.HorizontalDistance(point, destination)))
        {
            via = null;
        }
        return via;
    }

    public static bool IsDetourTooLong(Vector3 self, Vector3 destination, float routeLength)
        => routeLength > VectorMath.HorizontalDistance(self, destination) * MaxDetourRatio + DetourAllowanceYalms;

    private static Vector3? Pick(Vector3 self, Vector3 destination, in Persona persona, Func<Vector3, Vector3?> snap)
    {
        if (persona.RouteWander <= 0f)
        {
            return null;
        }

        var trip = VectorMath.HorizontalDistance(self, destination);
        if (trip < MinTripYalms || !HumanTiming.Maybe(BaseViaChance + WanderViaChance * persona.RouteWander))
        {
            return null;
        }
        if (VectorMath.FlatDirection(destination - self) is not { } forward)
        {
            return null;
        }

        var random = HumanTiming.SharedRandom;
        var along = MinAlong + (float)random.NextDouble() * (MaxAlong - MinAlong);
        var lateral = MathF.Min((MinLateralYalms + (float)random.NextDouble() * (MaxLateralYalms - MinLateralYalms)) * persona.RouteWander,
            trip * MaxLateralShare);
        var side = HumanTiming.Maybe(0.5) ? 1f : -1f;
        return snap(Vector3.Lerp(self, destination, along) + VectorMath.RightOf(forward) * (lateral * side));
    }
}

internal static class RouteShaper
{
    private const float StructureClearanceYalms = 1.5f;
    private const float StructureDetourExtraYalms = 1f;
    private const int MaxStructureDetours = 4;
    private const float MinTurnCosine = 0.94f;
    private const float SnapToleranceYalms = 0.3f;
    private const float MinVectorSq = 0.0001f;

    public static void SkirtStructures(List<Vector3> waypoints, IReadOnlyList<PvpStructure> structures, Func<Vector3, Vector3?> snap)
    {
        if (structures.Count == 0)
        {
            return;
        }

        var inserted = 0;
        for (var segmentIndex = 0; segmentIndex < waypoints.Count - 1 && inserted < MaxStructureDetours; segmentIndex++)
        {
            if (DetourAround(waypoints[segmentIndex], waypoints[segmentIndex + 1], structures, snap) is not { } detour)
            {
                continue;
            }
            waypoints.Insert(segmentIndex + 1, detour);
            inserted++;
        }
    }

    public static void RoundCorners(List<Vector3> waypoints, float rounding, Func<Vector3, Vector3?> snap)
    {
        if (rounding <= 0f)
        {
            return;
        }

        for (var cornerIndex = 1; cornerIndex < waypoints.Count - 1; cornerIndex++)
        {
            var corner = waypoints[cornerIndex];
            if (VectorMath.FlatDirection(corner - waypoints[cornerIndex - 1]) is not { } incoming
                || VectorMath.FlatDirection(waypoints[cornerIndex + 1] - corner) is not { } outgoing)
            {
                continue;
            }

            var cosine = Vector3.Dot(incoming, outgoing);
            if (cosine > MinTurnCosine || VectorMath.FlatDirection(incoming - outgoing) is not { } outward)
            {
                continue;
            }

            var candidate = corner + outward * (rounding * MathF.Min(1f, 1f - cosine));
            if (snap(candidate) is not { } snapped || VectorMath.HorizontalDistance(snapped, candidate) > SnapToleranceYalms)
            {
                continue;
            }
            waypoints[cornerIndex] = snapped;
        }
    }

    private static Vector3? DetourAround(Vector3 start, Vector3 end, IReadOnlyList<PvpStructure> structures, Func<Vector3, Vector3?> snap)
    {
        for (var structureIndex = 0; structureIndex < structures.Count; structureIndex++)
        {
            var structure = structures[structureIndex];
            var clearance = structure.Radius + StructureClearanceYalms;
            if (VectorMath.HorizontalDistance(start, structure.Position) < clearance
                || VectorMath.HorizontalDistance(end, structure.Position) < clearance)
            {
                continue;
            }

            var closest = ClosestOnSegment(start, end, structure.Position);
            var offset = closest - structure.Position;
            offset.Y = 0f;
            var distance = offset.Length();
            if (distance >= clearance)
            {
                continue;
            }

            var side = distance * distance > MinVectorSq
                ? offset / distance
                : VectorMath.RightOf(VectorMath.FlatDirection(end - start) ?? Vector3.UnitX);
            return snap((structure.Position + side * (clearance + StructureDetourExtraYalms)) with { Y = closest.Y });
        }
        return null;
    }

    private static Vector3 ClosestOnSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        var segmentX = end.X - start.X;
        var segmentZ = end.Z - start.Z;
        var lengthSquared = segmentX * segmentX + segmentZ * segmentZ;
        if (lengthSquared <= MinVectorSq)
        {
            return start;
        }

        var progress = ((point.X - start.X) * segmentX + (point.Z - start.Z) * segmentZ) / lengthSquared;
        return Vector3.Lerp(start, end, Math.Clamp(progress, 0f, 1f));
    }
}
