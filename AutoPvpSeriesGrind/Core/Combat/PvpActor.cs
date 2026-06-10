using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct PvpActor(
    ulong Id,
    Vector3 Position,
    float Hp,
    bool IsMelee,
    bool IsCasting,
    ulong TargetId,
    float DistanceToSelf);
