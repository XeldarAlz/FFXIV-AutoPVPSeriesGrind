namespace AutoPvpSeriesGrind.Core.Modes;

public static class SeriesGrindModes
{
    private static readonly List<ISeriesGrindMode> registered =
    [
        new MatchCountMode(),
        new SeriesRankMode(),
        new TimeBoxedMode(),
        new EndlessMode(),
    ];

    public static IReadOnlyList<ISeriesGrindMode> All => registered;

    public static ISeriesGrindMode Default => registered[0];

    public static ISeriesGrindMode GetById(string? id)
        => (id is null ? null : registered.FirstOrDefault(m => m.Id == id)) ?? Default;
}
