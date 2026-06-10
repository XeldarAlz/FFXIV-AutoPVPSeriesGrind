namespace AutoPvpSeriesGrind.Core.Modes;

public static class SeriesGrindModes
{
    private static readonly ISeriesGrindMode[] registered =
    [
        new MatchCountMode(),
        new SeriesRankMode(),
        new TimeBoxedMode(),
        new EndlessMode(),
    ];

    public static IReadOnlyList<ISeriesGrindMode> All => registered;

    public static ISeriesGrindMode Default => registered[0];

    public static ISeriesGrindMode GetById(string? id)
    {
        if (id is null)
        {
            return Default;
        }
        for (var modeIndex = 0; modeIndex < registered.Length; modeIndex++)
        {
            if (registered[modeIndex].Id == id)
            {
                return registered[modeIndex];
            }
        }
        return Default;
    }
}
