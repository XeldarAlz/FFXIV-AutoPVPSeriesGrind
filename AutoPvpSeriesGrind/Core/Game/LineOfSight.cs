using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class LineOfSight
{
    private const float EyeHeightYalms = 1.7f;

    public static bool IsVisible(Vector3 fromGround, Vector3 toGround)
    {
        var from = fromGround with { Y = fromGround.Y + EyeHeightYalms };
        var to = toGround with { Y = toGround.Y + EyeHeightYalms };
        return !Terrain.TryBlocked(from, to, out var blocked) || !blocked;
    }
}
