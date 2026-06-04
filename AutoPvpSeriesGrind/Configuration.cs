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

    // Optional gearset slot (1-based, as shown in the gear set list) to equip before queueing; 0 disables.
    public int GearsetSlot { get; set; } = 0;

    // Match social touches (ported from the script).
    public bool SendHelloOnEntry { get; set; } = true;
    public bool SendGoodMatchOnResults { get; set; } = true;
    public bool SetGaroTitles { get; set; } = false;

    // Optional Lifestream command run once before the first queue (e.g. travel to your PvP hub).
    public string LifestreamCommand { get; set; } = "";

    // Optional chat command executed once the match limit is reached (the script's "Follow-up script").
    public string FollowUpCommand { get; set; } = "";

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);

    public void SaveDebounced()
    {
        if (EzThrottler.Throttle(Core.ApsgConstants.ThrottleKeys.Save, Core.ApsgConstants.SaveThrottleMs))
            Save();
    }
}
