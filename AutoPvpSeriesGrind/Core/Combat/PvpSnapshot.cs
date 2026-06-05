using ECommons.GameFunctions;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class PvpSnapshot
{
    public required Vector3 Self { get; init; }
    public required ulong SelfId { get; init; }
    public required float SelfHp { get; init; }
    public required CombatRole SelfRole { get; init; }
    public required bool PrefersBackline { get; init; }
    public required Vector3? Objective { get; init; }
    public required IReadOnlyList<PvpActor> Enemies { get; init; }
    public required IReadOnlyList<PvpActor> Allies { get; init; }
    public required Vector3? EnemyCentroid { get; init; }
    public required Vector3? AllyCentroid { get; init; }
    public required int FocusCount { get; init; }
    public required uint Territory { get; init; }

    public bool HasObjective => Objective.HasValue;
}
