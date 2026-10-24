using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

// Fires the PvP limit break in any PvP duty, whether or not a grind run is active, the way the standalone
// Auto PVP LB plugin did before it was folded in here.
internal sealed class LimitBreakController : IDisposable
{
    public const string LegacyPluginInternalName = "PvpAutoLb";

    private const long TickIntervalMs = 33;
    private const long FiringHoldMs = 300;
    private const string DisablePluginCommand = "/xldisableplugin";

    private readonly TargetSwapper swapper = new();
    private readonly LbFirer firer;
    private readonly HpTracker hpTracker = new();
    private readonly FireDecisionMaker decisionMaker = new();
    private readonly List<IBattleChara> hostiles = new(16);
    private readonly List<IBattleChara> allies = new(8);
    private long nextTickAtMs;

    public bool LegacyPluginLoaded { get; private set; }

    // The rotation steps aside while this is true, so its next button press cannot replace the queued limit break.
    public bool Firing => Environment.TickCount64 - firer.LastSubmitAtMs < FiringHoldMs;

    public LimitBreakController()
    {
        firer = new LbFirer(swapper);
        RefreshLegacyPlugin();
        Svc.PluginInterface.ActivePluginsChanged += OnActivePluginsChanged;
        Svc.Framework.Update += OnUpdate;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
        Svc.PluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;
    }

    public void DisableLegacyPlugin()
    {
        if (!Svc.Commands.ProcessCommand($"{DisablePluginCommand} \"{LegacyPluginInternalName}\""))
        {
            RunLog.Warning($"{DisablePluginCommand} was not accepted; disable Auto PVP LB in /xlplugins.");
        }
    }

    private void OnActivePluginsChanged(IActivePluginsChangedEventArgs arguments) => RefreshLegacyPlugin();

    private void RefreshLegacyPlugin()
    {
        var loaded = false;
        foreach (var installedPlugin in Svc.PluginInterface.InstalledPlugins)
        {
            if (installedPlugin.IsLoaded && installedPlugin.InternalName == LegacyPluginInternalName)
            {
                loaded = true;
                break;
            }
        }

        if (loaded == LegacyPluginLoaded)
        {
            return;
        }

        LegacyPluginLoaded = loaded;
        RunLog.Info(loaded
            ? "limit break: the old Auto PVP LB plugin is loaded, so the built-in limit break stays off until it is disabled"
            : "limit break: the old Auto PVP LB plugin is gone, the built-in limit break takes over");
    }

    private void OnUpdate(IFramework framework)
    {
        try
        {
            Tick();
        }
        catch (Exception exception)
        {
            RunLog.Error(exception, "limit break tick failed");
        }
    }

    private void Tick()
    {
        swapper.TryRestore();

        var now = Environment.TickCount64;
        if (now < nextTickAtMs)
        {
            return;
        }

        nextTickAtMs = now + TickIntervalMs;
        var settings = Plugin.Cfg.LimitBreak;
        if (!settings.Enabled || LegacyPluginLoaded || !Svc.ClientState.IsPvP || Svc.Objects.LocalPlayer is not { } self
            || !LbDuty.IsAllowed(settings.EnabledDuties))
        {
            return;
        }

        var jobId = self.ClassJob.RowId;
        var profile = LbCatalog.ResolveProfile(jobId);
        if (profile.ActionId == 0)
        {
            return;
        }

        var rule = settings.RuleFor(jobId);
        Scan(self, rule);
        if (decisionMaker.Decide(in profile, rule, settings, self, hostiles, allies, hpTracker) is { } decision)
        {
            Fire(jobId, in decision, settings);
        }
    }

    private void Scan(IPlayerCharacter self, LbRule rule)
    {
        var scanRange = rule.Mode == LbFireMode.Offensive
            ? FireDecisionMaker.ScanRangeYalms
            : MathF.Max(FireDecisionMaker.ScanRangeYalms, MathF.Max(rule.EnemyRadiusYalms, rule.AllyRadiusYalms));
        TargetSelector.ScanHostiles(self, scanRange, hostiles);
        for (var hostileIndex = 0; hostileIndex < hostiles.Count; hostileIndex++)
        {
            hpTracker.Sample(hostiles[hostileIndex]);
        }

        if (rule.Mode == LbFireMode.Offensive)
        {
            allies.Clear();
            return;
        }

        TargetSelector.ScanAllies(self, scanRange, allies);
    }

    private void Fire(uint jobId, in FireDecision decision, LimitBreakOptions settings)
    {
        if (!firer.TryFire(jobId, decision.HardTarget, out var actionId))
        {
            return;
        }

        var actionName = LbCatalog.ActionName(actionId);
        RunLog.Info($"limit break: fired {actionName} ({actionId}) on 0x{decision.HardTarget.EntityId:X} (caught {decision.EnemiesAffected})");
        LbFeedback.OnFire(settings, actionName, decision.HardTarget.Name.TextValue);
    }
}
