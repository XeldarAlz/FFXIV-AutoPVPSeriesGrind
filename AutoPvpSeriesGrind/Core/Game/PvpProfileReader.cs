using AutoPvpSeriesGrind.Core.Util;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class PvpProfileReader
{
    public static int SeriesCurrentRank() => Safe.Try("SeriesCurrentRank read failed", () =>
    {
        var profile = PvPProfile.Instance();
        return profile == null ? 0 : profile->GetSeriesCurrentRank();
    }, fallback: 0);

    public static float SeriesRankProgress() => Safe.Try("SeriesRankProgress read failed", () =>
    {
        var profile = PvPProfile.Instance();
        if (profile == null)
        {
            return 0f;
        }
        int rank = profile->GetSeriesCurrentRank();
        if (rank < 1)
        {
            return 0f;
        }
        long expIntoCurrentRank = profile->GetSeriesExperience();
        long toNext = Svc.Data.GetExcelSheet<PvPSeriesLevel>()?.GetRowOrDefault((uint)rank)?.ExpToNext ?? 0;
        return toNext > 0 ? Math.Clamp(expIntoCurrentRank / (float)toNext, 0f, 1f) : 0f;
    }, fallback: 0f);

    public static long SeriesTotalExperience() => Safe.Try<long>("SeriesTotalExperience read failed", () =>
    {
        var profile = PvPProfile.Instance();
        if (profile == null)
        {
            return 0;
        }

        long total = profile->GetSeriesExperience();
        int rank = profile->GetSeriesCurrentRank();
        var sheet = Svc.Data.GetExcelSheet<PvPSeriesLevel>();
        if (sheet is not null)
        {
            for (uint rankIndex = 1; rankIndex < rank; rankIndex++)
            {
                total += sheet.GetRowOrDefault(rankIndex)?.ExpToNext ?? 0;
            }
        }
        return total;
    }, fallback: 0);
}
