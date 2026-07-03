using AutoPvpSeriesGrind.Core.Util;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class PvpProfileReader
{
    private const int CacheTtlMs = 500;

    private static long cacheStampMs;
    private static int cachedRank;
    private static float cachedProgress;
    private static long cachedTotalExp;

    public static int SeriesCurrentRank()
    {
        RefreshIfStale();
        return cachedRank;
    }

    public static float SeriesRankProgress()
    {
        RefreshIfStale();
        return cachedProgress;
    }

    public static long SeriesTotalExperience()
    {
        RefreshIfStale();
        return cachedTotalExp;
    }

    private static void RefreshIfStale()
    {
        var now = Environment.TickCount64;
        if (cacheStampMs != 0 && now - cacheStampMs < CacheTtlMs)
        {
            return;
        }
        cacheStampMs = now;
        cachedRank = ReadCurrentRank();
        cachedProgress = ReadRankProgress();
        cachedTotalExp = ReadTotalExperience();
    }

    private static int ReadCurrentRank() => Safe.Try("SeriesCurrentRank read failed", () =>
    {
        var profile = PvPProfile.Instance();
        return profile == null ? 0 : profile->GetSeriesCurrentRank();
    }, fallback: 0);

    private static float ReadRankProgress() => Safe.Try("SeriesRankProgress read failed", () =>
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

    private static long ReadTotalExperience() => Safe.Try<long>("SeriesTotalExperience read failed", () =>
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
