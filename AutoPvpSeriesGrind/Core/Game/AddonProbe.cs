using ECommons;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class AddonProbe
{
    public static bool IsReady(string addonName)
        => GenericHelpers.TryGetAddonByName<AtkUnitBase>(addonName, out var addon) && GenericHelpers.IsAddonReady(addon);

    public static bool TryGetReady(string addonName, out AtkUnitBase* addon)
        => GenericHelpers.TryGetAddonByName<AtkUnitBase>(addonName, out addon) && GenericHelpers.IsAddonReady(addon);
}
