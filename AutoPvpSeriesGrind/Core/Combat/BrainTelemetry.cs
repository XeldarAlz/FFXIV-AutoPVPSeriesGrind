namespace AutoPvpSeriesGrind.Core.Combat;

internal static class BrainTelemetry
{
    private const long FreshWindowMs = 3000;

    public static PvpSnapshot? Snapshot { get; private set; }
    public static MovePlan? Plan { get; private set; }
    public static long UpdatedTick { get; private set; }

    public static bool IsFresh => UpdatedTick != 0 && Environment.TickCount64 - UpdatedTick < FreshWindowMs;

    public static void Record(PvpSnapshot snapshot, in MovePlan plan)
    {
        Snapshot = snapshot;
        Plan = plan;
        UpdatedTick = Environment.TickCount64;
    }

    public static void RecordStatus(PvpSnapshot snapshot, MoveKind kind, string reason, Posture posture = Posture.Idle)
        => Record(snapshot, new MovePlan(kind, snapshot.Self, snapshot.Self, 0f, false, reason, false, posture));

    public static void Clear()
    {
        Snapshot = null;
        Plan = null;
        UpdatedTick = 0;
    }
}
