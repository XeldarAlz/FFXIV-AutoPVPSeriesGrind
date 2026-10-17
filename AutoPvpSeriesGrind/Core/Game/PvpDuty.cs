namespace AutoPvpSeriesGrind.Core.Game;

internal static class PvpDuty
{
    public const uint HiddenGorgeDutyId = 599;
    public const uint HiddenGorgeTerritoryId = 791;

    public static bool IsLargeScale(this MatchType mode)
        => mode is MatchType.Frontline or MatchType.RivalWings;

    public static bool Contains(MatchType mode, uint territory) => mode switch
    {
        MatchType.CrystallineConflict => CrystallineConflictMaps.Contains(territory),
        MatchType.Frontline => FrontlineMaps.Contains(territory),
        MatchType.RivalWings => territory == HiddenGorgeTerritoryId,
        _ => false,
    };

    public static string ResultsAddon(MatchType mode) => mode switch
    {
        MatchType.CrystallineConflict => "MKSRecord",
        MatchType.Frontline => "FrontlineRecord",
        MatchType.RivalWings => "ManeuversRecord",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}
