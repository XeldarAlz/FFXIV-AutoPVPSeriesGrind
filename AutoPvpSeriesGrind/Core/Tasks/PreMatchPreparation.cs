namespace AutoPvpSeriesGrind.Core.Tasks;

internal enum PreMatchDecision { Hold, Mount, Start }

// Being inside the duty is not a start signal. Wait for the duty to commence,
// allowing only mounting; a failed mount request must not be repeated every tick.
internal sealed class PreMatchPreparation
{
    private const int MountRetryMs = 2500;
    private long? lastMountAttemptAt;

    public PreMatchDecision Observe(bool dutyStarted, bool mounted, bool canMount, long nowMs)
    {
        if (dutyStarted) return PreMatchDecision.Start;
        if (mounted || !canMount || (lastMountAttemptAt is { } last && nowMs - last < MountRetryMs))
            return PreMatchDecision.Hold;

        lastMountAttemptAt = nowMs;
        return PreMatchDecision.Mount;
    }

    public void Reset() => lastMountAttemptAt = null;
}
