namespace AutoPvpSeriesGrind.Windows;

internal struct Transition
{
    private long markedTick;

    public void Mark() => markedTick = Environment.TickCount64;

    public readonly float Energy(double durationMs)
    {
        if (markedTick == 0)
            return 0f;
        var progress = (float)Math.Clamp((Environment.TickCount64 - markedTick) / durationMs, 0.0, 1.0);
        return 1f - Styling.EaseOutCubic(progress);
    }
}
