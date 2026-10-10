using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class Terrain
{
    private const float MinRaySq = 0.01f;
    private const float ProbeAboveYalms = 4f;
    private const float ProbeBelowYalms = 12f;

    private static readonly Vector3 Down = -Vector3.UnitY;

    public static bool TryBlocked(Vector3 from, Vector3 to, out bool blocked)
    {
        blocked = false;
        var direction = to - from;
        var distanceSq = direction.LengthSquared();
        if (distanceSq <= MinRaySq)
        {
            return true;
        }

        try
        {
            var distance = MathF.Sqrt(distanceSq);
            blocked = BGCollisionModule.RaycastMaterialFilter(from, direction / distance, out _, distance);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static float? GroundBelow(Vector3 point, float probeFromY)
    {
        try
        {
            var origin = point with { Y = probeFromY + ProbeAboveYalms };
            return BGCollisionModule.RaycastMaterialFilter(origin, Down, out var hit, ProbeAboveYalms + ProbeBelowYalms) ? hit.Point.Y : null;
        }
        catch
        {
            return null;
        }
    }
}
