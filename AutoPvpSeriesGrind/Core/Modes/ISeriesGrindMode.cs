namespace AutoPvpSeriesGrind.Core.Modes;

public readonly struct ModeContext
{
    public int MatchesCompleted { get; init; }
    public TimeSpan Elapsed { get; init; }
    public int SeriesRank { get; init; }
}

public interface ISeriesGrindMode
{
    string Id { get; }

    string DisplayName { get; }
    string Description { get; }

    bool IsComplete(ModeContext ctx);

    string? GetRemainingDisplay(ModeContext ctx) => null;
}
