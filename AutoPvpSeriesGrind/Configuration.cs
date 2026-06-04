using AutoPvpSeriesGrind.Core.Modes;
using Dalamud.Configuration;
using ECommons.Throttlers;

namespace AutoPvpSeriesGrind;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; }

    public bool AutoShowOnLogin { get; set; } = false;

    // Which stop condition is active; serialized by its stable Id. See Core/Modes.
    public string ModeId { get; set; } = MatchCountMode.ModeId;

    [Newtonsoft.Json.JsonIgnore]
    public ISeriesGrindMode ActiveMode => SeriesGrindModes.GetById(ModeId);

    // Per-mode targets.
    public int TargetMatchCount { get; set; } = 30;
    public int TargetSeriesRank { get; set; } = 15;
    public int TargetMinutes { get; set; } = 60;

    // Match social touches (ported from the script).
    public bool SendHelloOnEntry { get; set; } = true;
    public bool SendGoodMatchOnResults { get; set; } = true;

    // What to do once the run's goal is met (never fires on a manual Stop).
    public AfterRunAction AfterRun { get; set; } = AfterRunAction.StayLoggedIn;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public void SaveDebounced()
    {
        if (EzThrottler.Throttle(Core.ApsgConstants.ThrottleKeys.Save, Core.ApsgConstants.SaveThrottleMs))
            Save();
    }
}

public enum AfterRunAction
{
    StayLoggedIn,
    ReturnToInn,
    Logout,
    CloseGame,
}
