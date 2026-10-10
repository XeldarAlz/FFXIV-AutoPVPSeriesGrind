namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class BurstMeter
{
    private const long WindowMs = 1000;
    private const float WindowSeconds = WindowMs / 1000f;

    private readonly Queue<(long Tick, float Hp)> samples = new();

    public void Reset() => samples.Clear();

    public float DropPerSecond(float hp)
    {
        var now = Environment.TickCount64;
        while (samples.Count > 0 && now - samples.Peek().Tick > WindowMs)
        {
            samples.Dequeue();
        }

        var peak = hp;
        foreach (var sample in samples)
        {
            peak = MathF.Max(peak, sample.Hp);
        }
        samples.Enqueue((now, hp));
        return (peak - hp) / WindowSeconds;
    }
}
