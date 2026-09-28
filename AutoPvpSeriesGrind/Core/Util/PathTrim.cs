using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Util;

internal static class PathTrim
{
    public static void ShortenTail(List<Vector3> waypoints, float length)
    {
        var remaining = length;
        while (waypoints.Count >= 2 && remaining > 0f)
        {
            var last = waypoints[^1];
            var previous = waypoints[^2];
            var segmentLength = Vector3.Distance(previous, last);
            if (segmentLength > remaining)
            {
                waypoints[^1] = Vector3.Lerp(last, previous, remaining / segmentLength);
                return;
            }

            remaining -= segmentLength;
            waypoints.RemoveAt(waypoints.Count - 1);
        }

        if (waypoints.Count < 2)
        {
            waypoints.Clear();
        }
    }

    public static void DropPassed(List<Vector3> waypoints, Vector3 self, float maxSkipYalms)
    {
        var passedCount = 0;
        var bestDistance = float.MaxValue;
        var bestProgress = 0f;
        var walked = 0f;
        for (var segmentIndex = 0; segmentIndex < waypoints.Count - 1; segmentIndex++)
        {
            var start = waypoints[segmentIndex];
            var end = waypoints[segmentIndex + 1];
            var progress = SegmentProgress(self, start, end, out var distance);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestProgress = progress;
                passedCount = segmentIndex + 1;
            }

            walked += VectorMath.HorizontalDistance(start, end);
            if (walked > maxSkipYalms)
            {
                break;
            }
        }

        if (passedCount == waypoints.Count - 1 && bestProgress >= 1f)
        {
            waypoints.Clear();
            return;
        }

        waypoints.RemoveRange(0, passedCount);
    }

    private static float SegmentProgress(Vector3 point, Vector3 start, Vector3 end, out float distance)
    {
        var segmentX = end.X - start.X;
        var segmentZ = end.Z - start.Z;
        var lengthSquared = segmentX * segmentX + segmentZ * segmentZ;
        var progress = lengthSquared > 0f
            ? ((point.X - start.X) * segmentX + (point.Z - start.Z) * segmentZ) / lengthSquared
            : 1f;
        var clamped = Math.Clamp(progress, 0f, 1f);
        var offsetX = point.X - (start.X + segmentX * clamped);
        var offsetZ = point.Z - (start.Z + segmentZ * clamped);
        distance = MathF.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
        return progress;
    }
}
