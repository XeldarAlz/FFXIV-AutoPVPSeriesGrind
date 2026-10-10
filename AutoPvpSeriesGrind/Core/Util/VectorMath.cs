using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Util;

internal static class VectorMath
{
    private const float MinVectorSq = 0.0001f;

    public static float HorizontalDistance(Vector3 positionA, Vector3 positionB)
    {
        var deltaX = positionA.X - positionB.X;
        var deltaZ = positionA.Z - positionB.Z;
        return MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
    }

    public static Vector3? FlatDirection(Vector3 vector)
    {
        vector.Y = 0f;
        return vector.LengthSquared() > MinVectorSq ? Vector3.Normalize(vector) : null;
    }

    public static Vector3 RightOf(Vector3 forward) => new(forward.Z, 0f, -forward.X);

    public static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxStep)
    {
        var delta = target - current;
        var distance = delta.Length();
        return distance <= maxStep || distance <= 0f ? target : current + delta / distance * maxStep;
    }

    public static float PathLength(Vector3 start, List<Vector3> waypoints, int endIndex)
    {
        var length = 0f;
        var previous = start;
        for (var waypointIndex = 0; waypointIndex <= endIndex && waypointIndex < waypoints.Count; waypointIndex++)
        {
            length += HorizontalDistance(previous, waypoints[waypointIndex]);
            previous = waypoints[waypointIndex];
        }
        return length;
    }
}
