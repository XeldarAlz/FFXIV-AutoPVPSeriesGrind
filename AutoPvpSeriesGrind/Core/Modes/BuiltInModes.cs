namespace AutoPvpSeriesGrind.Core.Modes;

public sealed class MatchCountMode : ISeriesGrindMode
{
    public const string ModeId = "matches";
    public string Id => ModeId;
    public string DisplayName => "Run N matches";
    public string Description => "Stops after a fixed number of completed matches.";
    public bool IsComplete(ModeContext ctx) => ctx.MatchesCompleted >= Math.Max(1, Plugin.Cfg.TargetMatchCount);

    public string? GetRemainingDisplay(ModeContext ctx)
    {
        var left = Math.Max(0, Plugin.Cfg.TargetMatchCount - ctx.MatchesCompleted);
        return left > 0 ? $"{left} matches left" : null;
    }
}

public sealed class SeriesRankMode : ISeriesGrindMode
{
    public const string ModeId = "seriesrank";
    public string Id => ModeId;
    public string DisplayName => "Reach Series rank";
    public string Description => "Stops once your PvP Series (Malmstones) rank reaches the target.";
    public bool IsComplete(ModeContext ctx) => ctx.SeriesRank >= Plugin.Cfg.TargetSeriesRank;

    public string? GetRemainingDisplay(ModeContext ctx)
    {
        var target = Plugin.Cfg.TargetSeriesRank;
        return ctx.SeriesRank < target ? $"rank {ctx.SeriesRank} / {target}" : null;
    }
}

public sealed class TimeBoxedMode : ISeriesGrindMode
{
    public const string ModeId = "time";
    public string Id => ModeId;
    public string DisplayName => "Run for time";
    public string Description => "Runs for a set number of minutes, finishing the current match first.";
    public bool IsComplete(ModeContext ctx) => ctx.Elapsed >= TimeSpan.FromMinutes(Math.Max(1, Plugin.Cfg.TargetMinutes));

    public string? GetRemainingDisplay(ModeContext ctx)
    {
        var remaining = TimeSpan.FromMinutes(Math.Max(1, Plugin.Cfg.TargetMinutes)) - ctx.Elapsed;
        if (remaining <= TimeSpan.Zero) return null;
        return remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours}h {remaining.Minutes:D2}m left"
            : $"{remaining.Minutes}m {remaining.Seconds:D2}s left";
    }
}

public sealed class EndlessMode : ISeriesGrindMode
{
    public const string ModeId = "endless";
    public string Id => ModeId;
    public string DisplayName => "Endless";
    public string Description => "Keeps queueing matches until you press Stop.";
    public bool IsComplete(ModeContext ctx) => false;
    public string? GetRemainingDisplay(ModeContext ctx) => null;
}
