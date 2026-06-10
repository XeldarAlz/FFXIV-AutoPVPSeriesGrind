using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class PvpSnapshot
{
    public required Vector3 Self { get; init; }
    public required ulong SelfId { get; init; }
    public required float SelfHp { get; init; }
    public required bool PrefersBackline { get; init; }
    public required Vector3? Objective { get; init; }
    public required PvpActor? CurrentTarget { get; init; }
    public required IReadOnlyList<PvpActor> Enemies { get; init; }
    public required IReadOnlyList<PvpActor> Allies { get; init; }
    public required Vector3? EnemyCentroid { get; init; }
    public required Vector3? AllyCentroid { get; init; }
    public required int FocusCount { get; init; }

    public bool HasObjective => Objective.HasValue;

    public float NearestEnemyDistance => MinDistanceToSelf(Enemies);
    public float NearestAllyDistance => MinDistanceToSelf(Allies);

    public int AlliesWithin(float radius) => CountWithin(Allies, radius);
    public int EnemiesWithin(float radius) => CountWithin(Enemies, radius);

    public static int CountWithin(IReadOnlyList<PvpActor> actors, Vector3 center, float radius)
    {
        var count = 0;
        for (var actorIndex = 0; actorIndex < actors.Count; actorIndex++)
        {
            if (Vector3.Distance(actors[actorIndex].Position, center) <= radius)
            {
                count++;
            }
        }
        return count;
    }

    private static float MinDistanceToSelf(IReadOnlyList<PvpActor> actors)
    {
        var min = float.MaxValue;
        for (var actorIndex = 0; actorIndex < actors.Count; actorIndex++)
        {
            if (actors[actorIndex].DistanceToSelf < min)
            {
                min = actors[actorIndex].DistanceToSelf;
            }
        }
        return min;
    }

    private static int CountWithin(IReadOnlyList<PvpActor> actors, float radius)
    {
        var count = 0;
        for (var actorIndex = 0; actorIndex < actors.Count; actorIndex++)
        {
            if (actors[actorIndex].DistanceToSelf <= radius)
            {
                count++;
            }
        }
        return count;
    }
}
