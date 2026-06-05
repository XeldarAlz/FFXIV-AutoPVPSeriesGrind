namespace AutoPvpSeriesGrind.Core.Util;

internal static class HumanTiming
{
    public static readonly Random Rng = new();

    public static int Jitter(int baseMs, int spreadMs)
        => Math.Max(0, baseMs - spreadMs + Rng.Next(0, spreadMs * 2 + 1));

    public static int Reaction(int minMs = 180, int maxMs = 520)
    {
        if (maxMs <= minMs) return minMs;
        var span = maxMs - minMs;
        return minMs + (Rng.Next(0, span + 1) + Rng.Next(0, span + 1)) / 2;
    }

    public static bool Maybe(double probability) => Rng.NextDouble() < probability;

    public static float Offset(float max) => (float)((Rng.NextDouble() * 2.0 - 1.0) * max);

    public static (int Min, int Max) ReactionBand(HumanizeLevel level) => level switch
    {
        HumanizeLevel.Light => (80, 220),
        HumanizeLevel.Realistic => (140, 380),
        HumanizeLevel.Heavy => (260, 650),
        _ => (0, 0),
    };
}
