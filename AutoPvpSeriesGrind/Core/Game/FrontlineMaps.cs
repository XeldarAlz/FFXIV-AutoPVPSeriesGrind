namespace AutoPvpSeriesGrind.Core.Game;

internal static class FrontlineMaps
{
    private static readonly uint[] Territories = [376, 1273, 431, 554, 888, 1313];

    public static bool Contains(uint territoryId) => Array.IndexOf(Territories, territoryId) >= 0;
}
