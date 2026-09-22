using AutoPvpSeriesGrind.Core.Localization;
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
            Purpose: L.Plugins.PurposeVnavmesh,
            Required: true),
        [ExternalPlugin.Lifestream] = new(
            InternalName: "Lifestream",
            DisplayName: "Lifestream",
            RepoUrl: "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json",
            Purpose: L.Plugins.PurposeLifestream,
            Required: false),
        [ExternalPlugin.PvpAutoLb] = new(
            InternalName: "PvpAutoLb",
            DisplayName: "Auto PVP LB",
            RepoUrl: "https://raw.githubusercontent.com/XeldarAlz/DalamudPlugins/main/repo.json",
            Purpose: L.Plugins.PurposeAutoLb,
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

    public static bool IsRequired(ExternalPlugin plugin) => Catalog[plugin].Required;

    public static bool IsInstalled(ExternalPlugin plugin)
    {
        var info = Catalog[plugin];
        foreach (var installedPlugin in Svc.PluginInterface.InstalledPlugins)
        {
            if (!installedPlugin.IsLoaded)
            {
                continue;
            }
            if (installedPlugin.InternalName == info.InternalName)
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
            if (IsRequired(plugin) && !IsInstalled(plugin))
            {
                return false;
            }
        }
        return true;
    }
}
