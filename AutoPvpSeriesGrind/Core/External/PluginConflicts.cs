using AutoPvpSeriesGrind.Core.Localization;
using Dalamud.Interface;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.External;

internal static class PluginConflicts
{
    private static readonly PluginConflictCheck Check = new(ReadPlugins,
        exception => RunLog.Warning(exception, "Compatibility check failed; automation is blocked"));

    public static PluginConflictState Read() => Check.Read();
    public static bool Blocked => Read().Blocked;

    public static string Reason(PluginConflictState state)
        => Loc.T(state.DetectionFailed ? L.Conflicts.CheckFailed : L.Conflicts.StartReason);

    private static IEnumerable<ConflictPlugin> ReadPlugins()
    {
        foreach (var plugin in Svc.PluginInterface.InstalledPlugins)
        {
            if (plugin.IsLoaded)
                yield return new ConflictPlugin(plugin.InternalName, plugin.Name, plugin.Manifest.RepoUrl, true);
        }
    }

    public static void OpenPluginInstaller()
        => Svc.PluginInterface.OpenPluginInstallerTo(PluginInstallerOpenKind.InstalledPlugins, "Rotation Solver");

    public static bool DisableRotationSolver()
    {
        // Re-read on click; never infer that command acceptance means the asynchronous unload succeeded.
        var state = Read();
        if (!state.PluginLoaded) return false;
        try
        {
            foreach (var internalName in state.LoadedPluginInternalNames.Distinct(StringComparer.Ordinal))
            {
                // Only send a single validated identifier to Dalamud's command manager, never game chat.
                var command = PluginDisableCommand.Create(internalName);
                if (command is null || !Svc.Commands.ProcessCommand(command))
                    throw new InvalidOperationException("Dalamud did not accept the RSR disable command.");
            }
            return true;
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "Could not request disabling RSR");
            ApsgChat.PrintError(Loc.T(L.Conflicts.DisableFailed));
            OpenPluginInstaller();
            return false;
        }
    }
}
