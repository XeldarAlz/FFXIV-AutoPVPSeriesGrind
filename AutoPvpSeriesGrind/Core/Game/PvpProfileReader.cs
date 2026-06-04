using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoPvpSeriesGrind.Core.Game;

// Reads the current PvP Series (Malmstones) rank so the "reach rank N" stop mode can tell when it's done.
internal static unsafe class PvpProfileReader
{
    public static int SeriesCurrentRank()
    {
        try
        {
            var p = PvPProfile.Instance();
            return p == null ? 0 : p->GetSeriesCurrentRank();
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"{ApsgConstants.LogPrefix} SeriesCurrentRank read failed");
            return 0;
        }
    }
}
