using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoPvpSeriesGrind.Core.Game;

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
            ApsgLog.Warn(ex, "SeriesCurrentRank read failed");
            return 0;
        }
    }

    public static float SeriesRankProgress()
    {
        try
        {
            var p = PvPProfile.Instance();
            if (p == null) return 0f;
            int rank = p->GetSeriesCurrentRank();
            if (rank < 1) return 0f;
            long into = p->GetSeriesExperience();
            long toNext = Svc.Data.GetExcelSheet<PvPSeriesLevel>()?.GetRowOrDefault((uint)rank)?.ExpToNext ?? 0;
            return toNext > 0 ? Math.Clamp(into / (float)toNext, 0f, 1f) : 0f;
        }
        catch (Exception ex)
        {
            ApsgLog.Warn(ex, "SeriesRankProgress read failed");
            return 0f;
        }
    }

    public static long SeriesTotalExperience()
    {
        try
        {
            var p = PvPProfile.Instance();
            if (p == null) return 0;

            long total = p->GetSeriesExperience();
            int rank = p->GetSeriesCurrentRank();
            var sheet = Svc.Data.GetExcelSheet<PvPSeriesLevel>();
            if (sheet is not null)
                for (uint r = 1; r < rank; r++)
                    total += sheet.GetRowOrDefault(r)?.ExpToNext ?? 0;
            return total;
        }
        catch (Exception ex)
        {
            ApsgLog.Warn(ex, "SeriesTotalExperience read failed");
            return 0;
        }
    }
}
