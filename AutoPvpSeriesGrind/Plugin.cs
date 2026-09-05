using AutoPvpSeriesGrind.Core;
using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Stats;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows;
using AutoPvpSeriesGrind.Windows.Shell;
using clib;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind;

public sealed class Plugin : IDalamudPlugin
{
    private const string PrimaryCommandHelp = "Toggle the Auto PVP Series Grind window. /apsg config | stats | deps | about | brain | target | objects.";
    private const string AliasCommandHelp = "Alias for /apsg.";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    internal static Plugin Instance { get; private set; } = null!;

    internal Configuration Configuration { get; }
    internal static Configuration Cfg { get; private set; } = null!;
    internal WindowSystem WindowSystem { get; } = new("AutoPvpSeriesGrind");
    internal RunHistory History { get; }
    internal AutoPvpSeriesController Controller { get; }

    private readonly AppWindow appWindow;

    internal BrainDebugWindow BrainWindow { get; }

    private readonly EventHandler<UnobservedTaskExceptionEventArgs> unobservedTaskHandler;

    private readonly Dictionary<string, Action> subcommands;

    public Plugin()
    {
        Instance = this;

        ECommonsMain.Init(PluginInterface, this);
        CLibMain.Init(PluginInterface, this, CLibModule.Automation);

        unobservedTaskHandler = OnUnobservedTaskException;
        TaskScheduler.UnobservedTaskException += unobservedTaskHandler;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Cfg = Configuration;
        History = new RunHistory();
        Controller = new AutoPvpSeriesController();

        Fonts.Initialize(PluginInterface.UiBuilder, PluginDirectory);
        appWindow = new AppWindow(this);
        BrainWindow = new BrainDebugWindow();

        AddWindows();

        subcommands = BuildSubcommands();

        RegisterCommands();
        HookUiEvents();
    }

    private static string PluginDirectory => PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;

    private void AddWindows()
    {
        WindowSystem.AddWindow(appWindow);
        WindowSystem.AddWindow(BrainWindow);
    }

    private Dictionary<string, Action> BuildSubcommands() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["config"] = ToggleConfigUi,
        ["about"] = ToggleAboutUi,
        ["deps"] = ToggleDependenciesUi,
        ["dependencies"] = ToggleDependenciesUi,
        ["stats"] = ToggleHistoryUi,
        ["history"] = ToggleHistoryUi,
        ["brain"] = ToggleBrainUi,
        ["target"] = TargetDumper.Dump,
        ["objects"] = TargetDumper.DumpObjects,
    };

    private void RegisterCommands()
    {
        CommandManager.AddHandler(ApsgConstants.PrimaryCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = PrimaryCommandHelp,
        });
        CommandManager.AddHandler(ApsgConstants.AliasCommand, new CommandInfo(OnCommand)
        {
            HelpMessage = AliasCommandHelp,
        });
    }

    private void HookUiEvents()
    {
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Svc.Framework.Update += AutoInstallRequiredOnce;
    }

    private void AutoInstallRequiredOnce(IFramework framework)
    {
        Svc.Framework.Update -= AutoInstallRequiredOnce;
        ExternalPlugins.AutoInstallMissingRequired();
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Observed) return;
        if (e.Exception.ToString().Contains(ApsgConstants.VnavmeshIpcProviderMarker))
        {
            e.SetObserved();
            ApsgLog.Debug($"Observed vnavmesh IPC task fault: {e.Exception.GetBaseException().Message}");
        }
    }

    public void Dispose()
    {
        TaskScheduler.UnobservedTaskException -= unobservedTaskHandler;

        Svc.Framework.Update -= AutoInstallRequiredOnce;

        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();
        appWindow.Dispose();
        BrainWindow.Dispose();
        Fonts.Dispose();

        CommandManager.RemoveHandler(ApsgConstants.PrimaryCommand);
        CommandManager.RemoveHandler(ApsgConstants.AliasCommand);

        Controller.Stop();

        CLibMain.Dispose();
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        if (subcommands.TryGetValue(args.Trim(), out var action))
            action();
        else
            ToggleMainUi();
    }

    public void ToggleMainUi() => appWindow.TogglePage(AppWindow.Page.Grind);
    public void ToggleConfigUi() => appWindow.TogglePage(AppWindow.Page.Settings);
    public void ToggleAboutUi() => appWindow.TogglePage(AppWindow.Page.About);
    public void ToggleDependenciesUi() => appWindow.TogglePage(AppWindow.Page.Plugins);
    public void ToggleHistoryUi() => appWindow.TogglePage(AppWindow.Page.History);
    public void ToggleBrainUi() => BrainWindow.Toggle();
}
