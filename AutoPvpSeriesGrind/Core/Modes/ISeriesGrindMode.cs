namespace AutoPvpSeriesGrind.Core.Modes;

public readonly struct ModeContext
{
    public int MatchesCompleted { get; init; }
    public TimeSpan Elapsed { get; init; }
    public int SeriesRank { get; init; }
}

public interface ISeriesGrindMode
{
    // Stable serialization key — never change once shipped (persisted in config as ModeId).
    string Id { get; }

    string DisplayName { get; }
    string Description { get; }

    bool IsComplete(ModeContext ctx);

    // Short "X left" string for the status line, or null when there's nothing meaningful to show.
    string? GetRemainingDisplay(ModeContext ctx) => null;
}
