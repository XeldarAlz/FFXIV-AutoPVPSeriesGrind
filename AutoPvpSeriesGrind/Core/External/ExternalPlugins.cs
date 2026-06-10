using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.External;

internal static class ExternalPlugins
{
    public static readonly IReadOnlyDictionary<ExternalPlugin, ExternalPluginInfo> Catalog
        = new Dictionary<ExternalPlugin, ExternalPluginInfo>
    {
        [ExternalPlugin.Vnavmesh] = new(
            InternalName: "vnavmesh",
            DisplayName: "vnavmesh",
            RepoUrl: "https://puni.sh/api/repository/veyn",
            Purpose: "Pathfinding and movement to the objective during a match.",
            Required: true),
        [ExternalPlugin.RotationSolver] = new(
            InternalName: "RotationSolver",
            DisplayName: "RotationSolver Reborn",
            RepoUrl: "https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json",
            Purpose: "Drives combat during the match (/rotation auto LowHP).",
            Required: true,
            Aliases: ["RotationSolverReborn"]),
        [ExternalPlugin.Lifestream] = new(
            InternalName: "Lifestream",
            DisplayName: "Lifestream",
            RepoUrl: "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json",
            Purpose: "Optional: runs your configured travel command before the first queue.",
            Required: false),
        [ExternalPlugin.PvpAutoLb] = new(
            InternalName: "PvpAutoLb",
            DisplayName: "Auto PVP LB",
            RepoUrl: "https://raw.githubusercontent.com/XeldarAlz/FFXIV-AutoPVPLimitBreak/master/repo.json",
            Purpose: "Fires your PvP Limit Break. This plugin pushes proven per-class settings to it automatically.",
            Required: true),
    };

    private static readonly ExternalPlugin[] AllSnapshot = CreateAllSnapshot();

    public static IReadOnlyList<ExternalPlugin> All => AllSnapshot;

    private static ExternalPlugin[] CreateAllSnapshot()
    {
        var snapshot = new ExternalPlugin[Catalog.Count];
        var writeIndex = 0;
        foreach (var plugin in Catalog.Keys)
        {
            snapshot[writeIndex] = plugin;
            writeIndex++;
        }
        return snapshot;
    }

    public static bool IsInstalled(ExternalPlugin plugin)
    {
        var info = Catalog[plugin];
        foreach (var installedPlugin in Svc.PluginInterface.InstalledPlugins)
        {
            if (!installedPlugin.IsLoaded)
            {
                continue;
            }
            if (installedPlugin.InternalName == info.InternalName
                || (info.Aliases is not null && Array.IndexOf(info.Aliases, installedPlugin.InternalName) >= 0))
            {
                return true;
            }
        }
        return false;
    }

    public static bool AllRequiredInstalled()
    {
        for (var pluginIndex = 0; pluginIndex < AllSnapshot.Length; pluginIndex++)
        {
            var plugin = AllSnapshot[pluginIndex];
            if (!Catalog[plugin].Required)
            {
                continue;
            }
            if (!IsInstalled(plugin))
            {
                return false;
            }
        }
        return true;
    }

    public static void AutoInstallMissingRequired()
    {
        foreach (var plugin in All)
        {
            var info = Catalog[plugin];
            if (!info.Required || IsInstalled(plugin)) continue;
            if (PluginInstaller.IsInstalling(plugin) || PluginInstaller.DidFail(plugin)) continue;
            _ = PluginInstaller.Install(plugin);
        }
    }
}
