using ECommons.GameFunctions;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct PvpActor(
    ulong Id,
    Vector3 Position,
    float Hp,
    uint CurrentHp,
    CombatRole Role,
    bool IsMelee,
    bool IsCasting,
    ulong TargetId,
    float DistanceToSelf,
    float DistanceToObjective);
