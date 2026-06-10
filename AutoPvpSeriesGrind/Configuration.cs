using AutoPvpSeriesGrind.Core;
using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Modes;
using Dalamud.Configuration;
using ECommons.Throttlers;

namespace AutoPvpSeriesGrind;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public string ModeId { get; set; } = MatchCountMode.ModeId;

    [Newtonsoft.Json.JsonIgnore]
    public ISeriesGrindMode ActiveMode => SeriesGrindModes.GetById(ModeId);

    public int TargetMatchCount { get; set; } = 30;
    public int TargetSeriesRank { get; set; } = 15;
    public int TargetMinutes { get; set; } = 60;

    public bool SendHelloOnEntry { get; set; } = true;
    public bool SendGoodMatchOnResults { get; set; } = true;
    public int HelloChancePercent { get; set; } = 70;
    public int GoodMatchChancePercent { get; set; } = 60;
    public int HelloDelayMinSeconds { get; set; } = 2;
    public int HelloDelayMaxSeconds { get; set; } = 12;
    public int GoodMatchDelayMinSeconds { get; set; } = 0;
    public int GoodMatchDelayMaxSeconds { get; set; } = 2;
    public bool RandomEmotes { get; set; } = false;

    public RotationProvider RotationProvider { get; set; } = RotationProvider.RotationSolver;
    public bool EnableCombatBrain { get; set; } = true;
    public bool BrainPicksTargets { get; set; } = false;
    public PvpStrategy Strategy { get; set; } = PvpStrategy.Moderate;
    public CustomStrategyProfile CustomStrategy { get; set; } = new();

    public HumanizeLevel Humanize { get; set; } = HumanizeLevel.Realistic;

    public int LeaveDutyDelaySeconds { get; set; } = 1;

    public int RequeueDelayMinSeconds { get; set; } = 2;
    public int RequeueDelayMaxSeconds { get; set; } = 6;

    public bool TakeBreaks { get; set; } = false;
    public int BreakEveryMatches { get; set; } = 10;
    public int BreakMinutes { get; set; } = 5;

    public AfterRunAction AfterRun { get; set; } = AfterRunAction.StayLoggedIn;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public void SaveDebounced()
    {
        if (EzThrottler.Throttle(ApsgConstants.ThrottleKeys.Save, ApsgConstants.SaveThrottleMs))
        {
            Save();
        }
    }
}
