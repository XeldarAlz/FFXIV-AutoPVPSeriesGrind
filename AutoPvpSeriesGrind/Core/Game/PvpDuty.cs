namespace AutoPvpSeriesGrind.Core.Game;

internal static class PvpDuty
{
    public const uint HiddenGorgeDutyId = 599;
    public const uint HiddenGorgeTerritoryId = 791;

    private const uint FrontlineIntendedUse = 18;
    private const uint CrystallineConflictIntendedUse = 28;
    private const uint CrystallineConflictAltIntendedUse = 37;
    private const uint RivalWingsIntendedUse = 39;

    public static bool IsLargeScale(this MatchType mode)
        => mode is MatchType.Frontline or MatchType.RivalWings;

    public static bool Contains(MatchType mode, uint territory, uint intendedUse) => ModeOf(territory, intendedUse) == mode;

    public static MatchType? ModeOf(uint territory, uint intendedUse)
    {
        if (CrystallineConflictMaps.Contains(territory))
        {
            return MatchType.CrystallineConflict;
        }
        if (FrontlineMaps.Contains(territory))
        {
            return MatchType.Frontline;
        }
        if (territory == HiddenGorgeTerritoryId)
        {
            return MatchType.RivalWings;
        }

        return intendedUse switch
        {
            CrystallineConflictIntendedUse or CrystallineConflictAltIntendedUse => MatchType.CrystallineConflict,
            FrontlineIntendedUse => MatchType.Frontline,
            RivalWingsIntendedUse => MatchType.RivalWings,
            _ => null,
        };
    }

    public static string ResultsAddon(MatchType mode) => mode switch
    {
        MatchType.CrystallineConflict => "MKSRecord",
        MatchType.Frontline => "FrontlineRecord",
        MatchType.RivalWings => "ManeuversRecord",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };
}
