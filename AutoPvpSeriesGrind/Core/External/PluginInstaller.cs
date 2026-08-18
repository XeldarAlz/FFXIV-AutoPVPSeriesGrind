using Dalamud.Plugin;
using ECommons.DalamudServices;
using ECommons.Reflection;
using System.Reflection;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.External;

internal static class PluginInstaller
{
    // These string literals mirror Dalamud's internal PluginManager/manifest member names reached via reflection.
    private const string InternalNameProperty = "InternalName";
    private const string InstallPluginAsyncMethodName = "InstallPluginAsync";
    private const string RepoManifestParameterName = "repoManifest";
    private const string UseTestingParameterName = "useTesting";
    private const string ReasonParameterName = "reason";
    private const string TaskResultProperty = "Result";
    private const string IsLoadedProperty = "IsLoaded";

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
                : $"{info.DisplayName} install reported failure; repo may need to be added manually.");
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

    // Installs via Dalamud's internal PluginManager.InstallPluginAsync by reflection, binding
    // arguments to the live method's parameters by name so Dalamud signature changes cannot break
    // us the way ECommons.DalamudReflector's positional argument list does.
    private static async Task<bool> AddPlugin(string masterUrl, string internalName)
    {
        var manifest = await FetchManifest(masterUrl, internalName);
        if (manifest is null)
        {
            return false;
        }

        var pluginManager = DalamudReflector.GetPluginManager();
        if (pluginManager is null)
        {
            ApsgLog.Warn("Could not resolve Dalamud PluginManager");
            return false;
        }

        EnsureRepo(masterUrl);

        var installMethod = ResolveInstallMethod(pluginManager);
        if (installMethod is null)
        {
            return false;
        }

        var arguments = BindInstallArguments(installMethod, manifest);

        var installTask = (Task)installMethod.Invoke(pluginManager, arguments)!;
        await installTask.ConfigureAwait(false);

        return InstalledPluginIsLoaded(installTask);
    }

    private static async Task<object?> FetchManifest(string masterUrl, string internalName)
    {
        var plugins = await DalamudReflector.GetPluginMaster(masterUrl);
        if (plugins is null || plugins.Count == 0)
        {
            ApsgLog.Warn($"No manifests fetched from {masterUrl}");
            return null;
        }

        var manifest = plugins.FirstOrDefault(candidate => (string)candidate.GetFoP(InternalNameProperty) == internalName);
        if (manifest is null)
        {
            ApsgLog.Warn($"'{internalName}' not found in {masterUrl}");
            return null;
        }

        return manifest;
    }

    private static void EnsureRepo(string masterUrl)
    {
        if (!DalamudReflector.HasRepo(masterUrl))
        {
            DalamudReflector.AddRepo(masterUrl, true);
        }
        DalamudReflector.ReloadPluginMasters();
    }

    private static MethodInfo? ResolveInstallMethod(object pluginManager)
    {
        var installMethod = pluginManager.GetType().GetMethod(
            InstallPluginAsyncMethodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (installMethod is null)
        {
            ApsgLog.Warn("PluginManager.InstallPluginAsync not found");
        }
        return installMethod;
    }

    private static object?[] BindInstallArguments(MethodInfo installMethod, object manifest)
    {
        var parameters = installMethod.GetParameters();
        var arguments = new object?[parameters.Length];
        for (var argumentIndex = 0; argumentIndex < parameters.Length; argumentIndex++)
        {
            arguments[argumentIndex] = parameters[argumentIndex].Name switch
            {
                RepoManifestParameterName => manifest,
                UseTestingParameterName => false,
                ReasonParameterName => PluginLoadReason.Installer,
                _ => DefaultArgument(parameters[argumentIndex]),
            };
        }
        return arguments;
    }

    private static bool InstalledPluginIsLoaded(Task installTask)
    {
        var localPlugin = installTask.GetFoP(TaskResultProperty);
        return localPlugin is not null && (bool)localPlugin.GetFoP(IsLoadedProperty);
    }

    private static object? DefaultArgument(ParameterInfo parameter)
        => parameter.HasDefaultValue ? parameter.DefaultValue
            : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType)
            : null;
}
