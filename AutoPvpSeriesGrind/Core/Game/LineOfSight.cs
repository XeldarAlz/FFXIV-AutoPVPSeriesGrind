using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class LineOfSight
{
    private const float EyeHeightYalms = 1.7f;
    private const float MinRaySq = 0.01f;

    private const int VisionBlockingMaterialMask = 0x4000; // the game's vision-blocking material bit

    public static bool IsVisible(Vector3 fromGround, Vector3 toGround)
    {
        try
        {
            var from = fromGround with { Y = fromGround.Y + EyeHeightYalms };
            var to = toGround with { Y = toGround.Y + EyeHeightYalms };
            var direction = to - from;
            var distanceSq = direction.LengthSquared();
            if (distanceSq <= MinRaySq)
            {
                return true;
            }
            var distance = MathF.Sqrt(distanceSq);
            direction /= distance;

            var framework = Framework.Instance();
            if (framework == null || framework->BGCollisionModule == null)
            {
                return true;
            }

            int* materialFilter = stackalloc int[] { VisionBlockingMaterialMask, 0, VisionBlockingMaterialMask, 0 };
            RaycastHit hit;
            return !framework->BGCollisionModule->RaycastMaterialFilter(&hit, &from, &direction, distance, 1, materialFilter);
        }
        catch
        {
            return true;
        }
    }
}
