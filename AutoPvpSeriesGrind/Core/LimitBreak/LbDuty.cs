using AutoPvpSeriesGrind.Core.Game;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal static class LbDuty
{
    public static bool IsAllowed(DutyMask enabledDuties) => (enabledDuties & Current()) != 0;

    private static DutyMask Current() => PvpTerritory.Current() switch
    {
        MatchType.CrystallineConflict => DutyMask.CrystallineConflict,
        MatchType.Frontline => DutyMask.Frontline,
        MatchType.RivalWings => DutyMask.RivalWings,
        _ => DutyMask.Other,
    };
}
