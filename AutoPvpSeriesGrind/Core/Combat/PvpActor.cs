using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct PvpActor(
    ulong Id,
    Vector3 Position,
    float Hp,
    PvpRole Role,
    bool HasGuard,
    bool IsCasting,
    ulong TargetId,
    float DistanceToSelf)
{
    public bool IsMelee => Role is PvpRole.Tank or PvpRole.Melee;
}
