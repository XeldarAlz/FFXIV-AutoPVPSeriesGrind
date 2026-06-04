using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.External;

public enum ExternalPlugin
{
    Vnavmesh,
    RotationSolver,
    Lifestream,
}

public sealed record ExternalPluginInfo(
    string InternalName,
    string DisplayName,
    string RepoUrl,
    string Purpose,
    bool Required,
    // Alternate InternalNames (community forks with the same command/IPC surface).
    string[]? Aliases = null);

public static class ExternalPlugins
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
            RepoUrl: "https://raw.githubusercontent.com/FFXIV-CombatReborn/RotationSolverReborn/main/pluginmaster.json",
            Purpose: "Drives combat during the match (/rotation auto LowHP).",
            Required: true,
            Aliases: ["RotationSolverReborn"]),
        [ExternalPlugin.Lifestream] = new(
            InternalName: "Lifestream",
            DisplayName: "Lifestream",
            RepoUrl: "https://raw.githubusercontent.com/NightmareXIV/MyDalamudPlugins/main/pluginmaster.json",
            Purpose: "Optional: runs your configured travel command before the first queue.",
            Required: false),
    };

    public static IEnumerable<ExternalPlugin> All => Catalog.Keys;

    public static bool IsInstalled(ExternalPlugin plugin)
    {
        var info = Catalog[plugin];
        return Svc.PluginInterface.InstalledPlugins.Any(p =>
            p.IsLoaded
            && (p.InternalName == info.InternalName
                || (info.Aliases is not null && Array.IndexOf(info.Aliases, p.InternalName) >= 0)));
    }

    public static bool AllRequiredInstalled()
        => All.Where(p => Catalog[p].Required).All(IsInstalled);

    // No advisory "installed but disabled" state for these dependencies; kept for the DependencyRow contract.
    public static bool IsInstalledButDisabled(ExternalPlugin plugin) => false;
}
