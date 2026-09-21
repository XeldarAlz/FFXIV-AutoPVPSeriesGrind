using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class MarkingOps
{
    public static bool HasSign(ulong gameObjectId)
    {
        var markers = MarkingController.Instance()->Markers;
        for (var markerIndex = 0; markerIndex < markers.Length; markerIndex++)
        {
            if ((ulong)markers[markerIndex] == gameObjectId)
            {
                return true;
            }
        }
        return false;
    }
}
