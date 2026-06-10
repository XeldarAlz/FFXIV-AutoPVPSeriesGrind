using AutoPvpSeriesGrind.Core;
using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Stats;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows;
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

    private readonly MainWindow mainWindow;
    private readonly ConfigWindow configWindow;
    private readonly AboutWindow aboutWindow;
    private readonly DependenciesWindow dependenciesWindow;
    private readonly RunHistoryWindow runHistoryWindow;
    private readonly BrainDebugWindow brainWindow;

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

        mainWindow = new MainWindow(this);
        configWindow = new ConfigWindow(this);
        aboutWindow = new AboutWindow();
        dependenciesWindow = new DependenciesWindow();
        runHistoryWindow = new RunHistoryWindow();
        brainWindow = new BrainDebugWindow();

        AddWindows();

        subcommands = BuildSubcommands();

        RegisterCommands();
        HookUiEvents();
    }

    private void AddWindows()
    {
        WindowSystem.AddWindow(mainWindow);
        WindowSystem.AddWindow(configWindow);
        WindowSystem.AddWindow(aboutWindow);
        WindowSystem.AddWindow(dependenciesWindow);
        WindowSystem.AddWindow(runHistoryWindow);
        WindowSystem.AddWindow(brainWindow);
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
        mainWindow.Dispose();
        configWindow.Dispose();
        aboutWindow.Dispose();
        dependenciesWindow.Dispose();
        runHistoryWindow.Dispose();
        brainWindow.Dispose();

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

    public void ToggleMainUi() => mainWindow.Toggle();
    public void ToggleConfigUi() => configWindow.Toggle();
    public void ToggleAboutUi() => aboutWindow.Toggle();
    public void ToggleDependenciesUi() => dependenciesWindow.Toggle();
    public void ToggleHistoryUi() => runHistoryWindow.Toggle();
    public void ToggleBrainUi() => brainWindow.Toggle();
}
