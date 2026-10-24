using ECommons.DalamudServices;
using ECommons.ExcelServices;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal static class LbDuty
{
    private static uint cachedTerritory = uint.MaxValue;
    private static DutyMask cachedDuty;

    public static bool IsAllowed(DutyMask enabledDuties) => (enabledDuties & Current()) != 0;

    private static DutyMask Current()
    {
        var territory = (uint)Svc.ClientState.TerritoryType;
        if (territory != cachedTerritory)
        {
            cachedTerritory = territory;
            cachedDuty = ExcelTerritoryHelper.GetTerritoryIntendedUse(territory) switch
            {
                TerritoryIntendedUseEnum.Crystalline_Conflict or TerritoryIntendedUseEnum.Crystalline_Conflict_2 => DutyMask.CrystallineConflict,
                TerritoryIntendedUseEnum.Frontline => DutyMask.Frontline,
                TerritoryIntendedUseEnum.Rival_Wings => DutyMask.RivalWings,
                _ => DutyMask.Other,
            };
        }

        return cachedDuty;
    }
}
