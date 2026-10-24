using AutoPvpSeriesGrind.Core.Modes;

namespace AutoPvpSeriesGrind.Core.Localization;

internal static class PlanText
{
    public static LocString ModeName(Configuration cfg)
        => cfg.MatchType switch
        {
            MatchType.Frontline => L.Plan.ModeFrontline,
            MatchType.RivalWings => L.Plan.ModeRivalWings,
            _ => cfg.RankedCrystallineConflict ? L.Setup.ConflictRanked : L.Run.ModeCasual,
        };

    public static string StopSummary(Configuration cfg) => cfg.ActiveMode.Id switch
    {
        MatchCountMode.ModeId => Loc.T(L.Grind.StopsAfterMatches, cfg.TargetMatchCount),
        SeriesRankMode.ModeId => Loc.T(L.Grind.StopsAtRank, cfg.TargetSeriesRank),
        TimeBoxedMode.ModeId  => Loc.T(L.Grind.StopsAfterMinutes, cfg.TargetMinutes),
        _                     => Loc.T(L.Grind.StopsOnCommand),
    };
}
