using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct AllyCluster(Vector3 Centroid, int Size);

internal sealed class PvpSnapshot
{
    private const float ClusterLinkRadius = 15f; // allies within this of each other count as one fighting group

    public required Vector3 Self { get; init; }
    public required float SelfRotation { get; init; }
    public required ulong SelfId { get; init; }
    public required float SelfHp { get; init; }
    public required PvpRole SelfRole { get; init; }
    public required Vector3? Objective { get; init; }
    public required PvpActor? CurrentTarget { get; init; }
    public required IReadOnlyList<PvpActor> Enemies { get; init; }
    public required IReadOnlyList<PvpActor> Allies { get; init; }
    public required Vector3? EnemyCentroid { get; init; }
    public required int FocusCount { get; init; }

    private AllyCluster? allyCluster;
    private bool allyClusterComputed;

    public bool HasObjective => Objective.HasValue;
    public bool PrefersBackline => SelfRole is PvpRole.Ranged or PvpRole.Healer;

    public AllyCluster? AllyCluster
    {
        get
        {
            if (!allyClusterComputed)
            {
                allyCluster = LargestCluster(Allies, Self);
                allyClusterComputed = true;
            }
            return allyCluster;
        }
    }

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

    private static AllyCluster? LargestCluster(IReadOnlyList<PvpActor> allies, Vector3 self)
    {
        if (allies.Count == 0)
        {
            return null;
        }

        var groupIds = new int[allies.Count];
        for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            groupIds[allyIndex] = allyIndex;
        }
        for (var firstIndex = 0; firstIndex < allies.Count; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < allies.Count; secondIndex++)
            {
                if (groupIds[firstIndex] == groupIds[secondIndex]
                    || Vector3.Distance(allies[firstIndex].Position, allies[secondIndex].Position) > ClusterLinkRadius)
                {
                    continue;
                }
                var obsolete = groupIds[secondIndex];
                for (var memberIndex = 0; memberIndex < allies.Count; memberIndex++)
                {
                    if (groupIds[memberIndex] == obsolete)
                    {
                        groupIds[memberIndex] = groupIds[firstIndex];
                    }
                }
            }
        }

        var sums = new Vector3[allies.Count];
        var counts = new int[allies.Count];
        for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            sums[groupIds[allyIndex]] += allies[allyIndex].Position;
            counts[groupIds[allyIndex]]++;
        }

        AllyCluster? best = null;
        var bestDistance = float.MaxValue;
        for (var label = 0; label < allies.Count; label++)
        {
            if (counts[label] == 0)
            {
                continue;
            }
            var centroid = sums[label] / counts[label];
            var distance = Vector3.Distance(self, centroid);
            if (best is not { } current
                || counts[label] > current.Size
                || (counts[label] == current.Size && distance < bestDistance))
            {
                best = new AllyCluster(centroid, counts[label]);
                bestDistance = distance;
            }
        }
        return best;
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
