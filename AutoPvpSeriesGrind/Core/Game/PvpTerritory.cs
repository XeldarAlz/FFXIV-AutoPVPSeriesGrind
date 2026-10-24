using ECommons.DalamudServices;
using ECommons.ExcelServices;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class PvpTerritory
{
    private static uint cachedTerritory = uint.MaxValue;
    private static MatchType? cachedMode;

    public static MatchType? Current() => ModeOf(Svc.ClientState.TerritoryType);

    public static MatchType? ModeOf(uint territory)
    {
        if (territory != cachedTerritory)
        {
            cachedTerritory = territory;
            cachedMode = PvpDuty.ModeOf(territory, (uint)ExcelTerritoryHelper.GetTerritoryIntendedUse(territory));
        }

        return cachedMode;
    }
}
