using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal enum MoveKind { Hold, Engage, Retreat }

// Posture is the high-level intent shown in the overlay; MoveKind is how the executor actually moves.
internal enum Posture { Idle, Hold, Push, Stage, Reposition, Regroup, Retreat }

internal readonly record struct MovePlan(
    MoveKind Kind,
    Vector3 Destination,
    Vector3 Fallback,
    float StopRange,
    bool Sprint,
    string Reason,
    bool Pursue = false,
    Posture Posture = Posture.Idle,
    ulong TargetId = 0);
