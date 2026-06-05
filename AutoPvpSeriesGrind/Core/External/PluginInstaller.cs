using Dalamud.Plugin;
using ECommons.DalamudServices;
using ECommons.Reflection;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.External;

public static class PluginInstaller
{
    private static readonly HashSet<ExternalPlugin> InFlight = [];

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
            ApsgLog.Info($"Installing {info.DisplayName} from {info.RepoUrl}");
            var ok = await AddPlugin(info.RepoUrl, info.InternalName);
            ApsgLog.Info(ok
                ? $"{info.DisplayName} installed."
                : $"{info.DisplayName} install reported failure — repo may need to be added manually.");
            if (!ok) Failed.Add(plugin);
            return ok;
        }
        catch (Exception ex)
        {
            ApsgLog.Warn(ex, "plugin install threw");
            Failed.Add(plugin);
            return false;
        }
        finally
        {
            InFlight.Remove(plugin);
        }
    }

    // Installs via Dalamud's internal PluginManager.InstallPluginAsync by reflection. We bind the
    // arguments to the live method's parameters by name and length so we survive Dalamud signature
    // drift — ECommons.DalamudReflector.AddPlugin hard-codes a 4-arg call that throws "Parameter
    // count mismatch" once Dalamud changed InstallPluginAsync to 3 parameters.
    private static async Task<bool> AddPlugin(string masterUrl, string internalName)
    {
        var plugins = await DalamudReflector.GetPluginMaster(masterUrl);
        if (plugins is null || plugins.Count == 0)
        {
            ApsgLog.Warn($"No manifests fetched from {masterUrl}");
            return false;
        }

        var manifest = plugins.FirstOrDefault(x => (string)x.GetFoP("InternalName") == internalName);
        if (manifest is null)
        {
            ApsgLog.Warn($"'{internalName}' not found in {masterUrl}");
            return false;
        }

        var pm = DalamudReflector.GetPluginManager();
        if (pm is null)
        {
            ApsgLog.Warn("Could not resolve Dalamud PluginManager");
            return false;
        }

        if (!DalamudReflector.HasRepo(masterUrl))
            DalamudReflector.AddRepo(masterUrl, true);
        DalamudReflector.ReloadPluginMasters();

        var method = pm.GetType().GetMethod(
            "InstallPluginAsync",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method is null)
        {
            ApsgLog.Warn("PluginManager.InstallPluginAsync not found");
            return false;
        }

        var pars = method.GetParameters();
        var args = new object?[pars.Length];
        for (var i = 0; i < pars.Length; i++)
        {
            args[i] = pars[i].Name switch
            {
                "repoManifest" => manifest,
                "useTesting" => false,
                "reason" => PluginLoadReason.Installer,
                _ => DefaultArg(pars[i]),
            };
        }

        var task = (Task)method.Invoke(pm, args)!;
        await task.ConfigureAwait(false);

        var localPlugin = task.GetFoP("Result");
        return localPlugin is not null && (bool)localPlugin.GetFoP("IsLoaded");
    }

    private static object? DefaultArg(ParameterInfo p)
        => p.HasDefaultValue ? p.DefaultValue
            : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType)
            : null;
}
