using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace AutoPvpSeriesGrind.Core.Game;

// Equips a configured gearset before queueing, mirroring the script's optional Gearset Slot setting.
internal static unsafe class GearsetOps
{
    // userSlot is 1-based as shown in the gearset UI; 0 disables. Returns true if nothing to do or the
    // equip succeeded.
    public static bool EquipSlot(int userSlot)
    {
        if (userSlot < 1) return true;
        var apiIndex = userSlot - 1;
        try
        {
            var mod = RaptureGearsetModule.Instance();
            if (mod == null) return false;
            if (!mod->IsValidGearset(apiIndex))
            {
                Svc.Log.Warning($"{ApsgConstants.LogPrefix} gearset slot {userSlot} is invalid/empty");
                return false;
            }
            if (mod->CurrentGearsetIndex == apiIndex) return true;

            // 2nd arg = glamour plate id; 0 keeps the gearset's linked plate.
            var result = mod->EquipGearset(apiIndex, 0);
            if (result != 0)
            {
                Svc.Log.Warning($"{ApsgConstants.LogPrefix} EquipGearset({apiIndex}) returned {result}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"{ApsgConstants.LogPrefix} EquipSlot failed");
            return false;
        }
    }
}
