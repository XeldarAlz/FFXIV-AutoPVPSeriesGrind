using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Util;

internal static class VectorMath
{
    public static float HorizontalDistance(Vector3 positionA, Vector3 positionB)
    {
        var deltaX = positionA.X - positionB.X;
        var deltaZ = positionA.Z - positionB.Z;
        return MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
    }
}
