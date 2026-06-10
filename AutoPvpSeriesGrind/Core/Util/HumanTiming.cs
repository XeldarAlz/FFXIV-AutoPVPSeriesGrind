namespace AutoPvpSeriesGrind.Core.Util;

internal static class HumanTiming
{
    public static readonly Random SharedRandom = new();

    public static int Jitter(int baseMs, int spreadMs)
        => Math.Max(0, baseMs - spreadMs + SharedRandom.Next(0, spreadMs * 2 + 1));

    public static int Reaction(int minMs, int maxMs)
    {
        if (maxMs <= minMs)
        {
            return minMs;
        }

        var span = maxMs - minMs;
        return minMs + (SharedRandom.Next(0, span + 1) + SharedRandom.Next(0, span + 1)) / 2;
    }

    public static bool Maybe(double probability) => SharedRandom.NextDouble() < probability;

    public static int RandomSecondsInclusive(int minSeconds, int maxSeconds)
    {
        var clampedMinSeconds = Math.Max(0, minSeconds);
        var clampedMaxSeconds = Math.Max(clampedMinSeconds, maxSeconds);
        return clampedMinSeconds == clampedMaxSeconds ? clampedMinSeconds : SharedRandom.Next(clampedMinSeconds, clampedMaxSeconds + 1);
    }

    public static (int Min, int Max) ReactionBand(HumanizeLevel level) => level switch
    {
        HumanizeLevel.Light => (80, 220),
        HumanizeLevel.Realistic => (140, 380),
        HumanizeLevel.Heavy => (260, 650),
        _ => (0, 0),
    };
}
