using AutoPvpSeriesGrind.Core;
using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Localization;
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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    internal static Plugin Instance { get; private set; } = null!;

    internal Configuration Configuration { get; }
    internal static Configuration Cfg { get; private set; } = null!;
    internal WindowSystem WindowSystem { get; } = new("AutoPvpSeriesGrind");
    internal RunHistory History { get; }
    internal AutoPvpSeriesController Controller { get; }

    private readonly AppWindow appWindow;

    private readonly EventHandler<UnobservedTaskExceptionEventArgs> unobservedTaskHandler;

    private readonly Dictionary<string, Action> subcommands;

    private readonly CommandInfo primaryCommand;
    private readonly CommandInfo aliasCommand;

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

        InitializeLocalization();
        Fonts.Initialize(PluginInterface.UiBuilder, PluginDirectory);
        appWindow = new AppWindow(this);
        WindowSystem.AddWindow(appWindow);

        subcommands = BuildSubcommands();

        primaryCommand = new CommandInfo(OnCommand) { HelpMessage = Loc.T(L.Plugin.CommandHelp) };
        aliasCommand = new CommandInfo(OnCommand) { HelpMessage = Loc.T(L.Plugin.CommandHelpAlias) };

        RegisterCommands();
        HookUiEvents();
    }

    private static string PluginDirectory => PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;

    public void OnLanguageChanged()
    {
        primaryCommand.HelpMessage = Loc.T(L.Plugin.CommandHelp);
        aliasCommand.HelpMessage = Loc.T(L.Plugin.CommandHelpAlias);
    }

    private static void InitializeLocalization()
    {
        var directory = Path.Combine(PluginDirectory, "Localization");
        if (string.IsNullOrEmpty(Cfg.Language))
        {
            Cfg.Language = DetectLanguage();
            Cfg.Save();
        }

        Loc.Initialize(Cfg.Language, directory);
    }

    private static string DetectLanguage()
    {
        var dalamudLanguage = PluginInterface.UiLanguage;
        if (Languages.IsKnown(dalamudLanguage)) return Languages.Resolve(dalamudLanguage).Code;

        switch (Svc.ClientState.ClientLanguage)
        {
            case Dalamud.Game.ClientLanguage.German: return Languages.German.Code;
            case Dalamud.Game.ClientLanguage.French: return Languages.French.Code;
            case Dalamud.Game.ClientLanguage.Japanese: return Languages.Japanese.Code;
        }

        var osLanguage = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
        return Languages.IsKnown(osLanguage) ? Languages.Resolve(osLanguage).Code : Languages.English.Code;
    }

    private Dictionary<string, Action> BuildSubcommands() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["config"] = ToggleConfigUi,
        ["about"] = ToggleAboutUi,
        ["deps"] = ToggleDependenciesUi,
        ["dependencies"] = ToggleDependenciesUi,
        ["stats"] = ToggleHistoryUi,
        ["history"] = ToggleHistoryUi,
        ["target"] = TargetDumper.Dump,
        ["objects"] = TargetDumper.DumpObjects,
    };

    private void RegisterCommands()
    {
        CommandManager.AddHandler(ApsgConstants.PrimaryCommand, primaryCommand);
        CommandManager.AddHandler(ApsgConstants.AliasCommand, aliasCommand);
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
}
