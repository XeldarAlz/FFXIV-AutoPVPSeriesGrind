namespace AutoPvpSeriesGrind.Core.Tasks;

internal enum QueueState { Unknown, Idle, Registering, Queued, Ready, Accepted, InDuty }
internal enum QueueDecision { Wait, Submit, InFlight, Backoff, Failed }

// A native call returning does not mean the server accepted registration.
internal sealed class QueueSubmission
{
    public const int RegistrationTimeoutMs = 10_000;
    public const int UnknownTimeoutMs = 30_000;
    public const int MaxAttempts = 3;
    public const int MaxCycles = 3;
    public const int CycleBackoffMs = 60_000;
    public int Attempts { get; private set; }
    public bool HasSubmitted { get; private set; }
    private int failedCycles;
    private long? submittedAt;
    private long? unknownSince;
    private long? backoffUntil;

    public long BackoffRemainingMs(long now) => backoffUntil is { } until && until > now ? until - now : 0;

    public QueueDecision Observe(QueueState state, long now)
    {
        if (state == QueueState.Unknown)
        {
            unknownSince ??= now;
            return now - unknownSince.Value >= UnknownTimeoutMs ? QueueDecision.Failed : QueueDecision.Wait;
        }
        unknownSince = null;
        if (state == QueueState.Registering)
        {
            submittedAt ??= now;
            return now - submittedAt.Value >= RegistrationTimeoutMs ? QueueDecision.Failed : QueueDecision.InFlight;
        }
        if (state != QueueState.Idle)
        {
            submittedAt = null;
            backoffUntil = null;
            Attempts = 0;
            failedCycles = 0;
            return QueueDecision.InFlight;
        }
        if (submittedAt is { } sent && now - sent < RegistrationTimeoutMs) return QueueDecision.Wait;
        if (backoffUntil is { } until)
        {
            if (now < until) return QueueDecision.Backoff;
            backoffUntil = null;
            Attempts = 0;
        }
        if (Attempts < MaxAttempts) return QueueDecision.Submit;

        failedCycles++;
        if (failedCycles >= MaxCycles) return QueueDecision.Failed;
        backoffUntil = now + CycleBackoffMs;
        return QueueDecision.Backoff;
    }

    public void Submitted(long now, bool dispatched)
    {
        Attempts++;
        submittedAt = now;
        HasSubmitted |= dispatched;
    }

    public void Reset()
    {
        Attempts = 0;
        HasSubmitted = false;
        failedCycles = 0;
        submittedAt = null;
        unknownSince = null;
        backoffUntil = null;
    }
}
