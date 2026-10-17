namespace AutoPvpSeriesGrind.Core.Tasks;

internal enum QueueState { Unknown, Idle, Registering, Queued, Ready, Accepted, InDuty }
internal enum QueueDecision { Wait, Submit, InFlight, Failed }

// A native call returning does not mean the server accepted registration.
internal sealed class QueueSubmission
{
    public const int RegistrationTimeoutMs = 10_000;
    public const int UnknownTimeoutMs = 30_000;
    public const int MaxAttempts = 3;
    public int Attempts { get; private set; }
    public bool HasSubmitted { get; private set; }
    private long? submittedAt;
    private long? unknownSince;

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
            Attempts = 0;
            return QueueDecision.InFlight;
        }
        if (submittedAt is { } sent && now - sent < RegistrationTimeoutMs) return QueueDecision.Wait;
        return Attempts >= MaxAttempts ? QueueDecision.Failed : QueueDecision.Submit;
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
        submittedAt = null;
        unknownSince = null;
    }
}
