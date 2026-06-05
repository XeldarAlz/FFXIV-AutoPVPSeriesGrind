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
    public required PvpActor? CurrentTarget { get; init; }
    public required IReadOnlyList<PvpActor> Enemies { get; init; }
    public required IReadOnlyList<PvpActor> Allies { get; init; }
    public required Vector3? EnemyCentroid { get; init; }
    public required Vector3? AllyCentroid { get; init; }
    public required int FocusCount { get; init; }
    public required uint Territory { get; init; }

    public bool HasObjective => Objective.HasValue;

    public float NearestEnemyDistance => MinDistanceToSelf(Enemies);
    public float NearestAllyDistance => MinDistanceToSelf(Allies);

    public int AlliesWithin(float radius) => CountWithin(Allies, radius);
    public int EnemiesWithin(float radius) => CountWithin(Enemies, radius);

    private static float MinDistanceToSelf(IReadOnlyList<PvpActor> actors)
    {
        var min = float.MaxValue;
        foreach (var a in actors)
            if (a.DistanceToSelf < min) min = a.DistanceToSelf;
        return min;
    }

    private static int CountWithin(IReadOnlyList<PvpActor> actors, float radius)
    {
        var n = 0;
        foreach (var a in actors)
            if (a.DistanceToSelf <= radius) n++;
        return n;
    }
}
