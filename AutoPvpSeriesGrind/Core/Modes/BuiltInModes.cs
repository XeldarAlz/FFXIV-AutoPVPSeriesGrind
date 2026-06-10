namespace AutoPvpSeriesGrind.Core.Modes;

public sealed class MatchCountMode : ISeriesGrindMode
{
    public const string ModeId = "matches";
    public string Id => ModeId;
    public string DisplayName => "Run N matches";
    public string Description => "Stops after a fixed number of completed matches.";
    public bool IsComplete(ModeContext ctx) => ctx.MatchesCompleted >= Math.Max(1, Plugin.Cfg.TargetMatchCount);
}

public sealed class SeriesRankMode : ISeriesGrindMode
{
    public const string ModeId = "seriesrank";
    public string Id => ModeId;
    public string DisplayName => "Reach Series rank";
    public string Description => "Stops once your PvP Series (Malmstones) rank reaches the target.";
    public bool IsComplete(ModeContext ctx) => ctx.SeriesRank >= Plugin.Cfg.TargetSeriesRank;
}

public sealed class TimeBoxedMode : ISeriesGrindMode
{
    public const string ModeId = "time";
    public string Id => ModeId;
    public string DisplayName => "Run for time";
    public string Description => "Runs for a set number of minutes, finishing the current match first.";
    public bool IsComplete(ModeContext ctx) => ctx.Elapsed >= TimeSpan.FromMinutes(Math.Max(1, Plugin.Cfg.TargetMinutes));
}

public sealed class EndlessMode : ISeriesGrindMode
{
    public const string ModeId = "endless";
    public string Id => ModeId;
    public string DisplayName => "Endless";
    public string Description => "Keeps queueing matches until you press Stop.";
    public bool IsComplete(ModeContext ctx) => false;
}
