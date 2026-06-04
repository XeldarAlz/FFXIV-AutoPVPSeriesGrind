using ECommons.DalamudServices;
using ECommons.Reflection;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.External;

public static class PluginInstaller
{
    private static readonly HashSet<ExternalPlugin> InFlight = [];

    // Last attempt failed and the plugin is still not installed — drives the "Retry"/failure hint in DependencyRow.
    // Cleared when an install succeeds or the user starts a new attempt.
    private static readonly HashSet<ExternalPlugin> Failed = [];

    public static bool IsInstalling(ExternalPlugin plugin) => InFlight.Contains(plugin);

    public static bool DidFail(ExternalPlugin plugin) => Failed.Contains(plugin);

    public static async Task<bool> Install(ExternalPlugin plugin)
    {
        if (!InFlight.Add(plugin)) return false;
        Failed.Remove(plugin);
        try
        {
            var info = ExternalPlugins.Catalog[plugin];
            Svc.Log.Info($"[ExternalPlugin] Installing {info.DisplayName} from {info.RepoUrl}");
            var ok = await DalamudReflector.AddPlugin(info.RepoUrl, info.InternalName);
            Svc.Log.Info(ok
                ? $"[ExternalPlugin] {info.DisplayName} installed."
                : $"[ExternalPlugin] {info.DisplayName} install reported failure — repo may need to be added manually.");
            if (!ok) Failed.Add(plugin);
            return ok;
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[ExternalPlugin] install threw");
            Failed.Add(plugin);
            return false;
        }
        finally
        {
            InFlight.Remove(plugin);
        }
    }
}
