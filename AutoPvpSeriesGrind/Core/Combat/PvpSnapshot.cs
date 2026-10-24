using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct AllyCluster(Vector3 Centroid, int Size, int MountedCount, Vector3 Densest)
{
    public bool IsRiding => MountedCount * 2 > Size;
}

internal sealed class PvpSnapshot
{
    private const float ClusterLinkRadius = 15f; // allies within this of each other count as one fighting group
    private const float DensityKernelYalms = 24f;
    private const float DensestCoreYalms = 12f;
    private const int StackScratchLimit = 64;

    public required Vector3 Self { get; init; }
    public required float SelfRotation { get; init; }
    public required ulong SelfId { get; init; }
    public required float SelfHp { get; init; }
    public required PvpRole SelfRole { get; init; }
    public required Vector3? Objective { get; init; }
    public required PvpActor? CurrentTarget { get; init; }
    public required IReadOnlyList<PvpActor> Enemies { get; init; }
    public required IReadOnlyList<PvpActor> Allies { get; init; }
    public IReadOnlyList<PvpActor>? RegroupAllies { get; init; }
    public required Vector3? EnemyCentroid { get; init; }
    public required int FocusCount { get; init; }
    public required IReadOnlyList<Hazard> Hazards { get; init; }
    public required IReadOnlyList<PvpStructure> Structures { get; init; }

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
                allyCluster = Largest(Clusters(Allies), Self);
                allyClusterComputed = true;
            }
            return allyCluster;
        }
    }

    public bool InHazard(Vector3 point, float margin, out Hazard hazard)
    {
        for (var hazardIndex = 0; hazardIndex < Hazards.Count; hazardIndex++)
        {
            if (Hazards[hazardIndex].Contains(point, margin))
            {
                hazard = Hazards[hazardIndex];
                return true;
            }
        }
        hazard = default;
        return false;
    }

    public float NearestEnemyDistance => MinDistanceToSelf(Enemies);
    public PvpActor? NearestEnemy => NearestToSelf(Enemies);
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

    private static PvpActor? NearestToSelf(IReadOnlyList<PvpActor> actors)
    {
        PvpActor? nearest = null;
        for (var actorIndex = 0; actorIndex < actors.Count; actorIndex++)
        {
            if (nearest is null || actors[actorIndex].DistanceToSelf < nearest.Value.DistanceToSelf)
            {
                nearest = actors[actorIndex];
            }
        }
        return nearest;
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

    public static List<AllyCluster> Clusters(IReadOnlyList<PvpActor> allies)
    {
        var clusters = new List<AllyCluster>();
        var count = allies.Count;
        if (count == 0)
        {
            return clusters;
        }

        var onStack = count <= StackScratchLimit;
        Span<int> groupIds = onStack ? stackalloc int[count] : new int[count];
        Span<Vector3> sums = onStack ? stackalloc Vector3[count] : new Vector3[count];
        Span<int> counts = onStack ? stackalloc int[count] : new int[count];
        Span<int> mountedCounts = onStack ? stackalloc int[count] : new int[count];
        Span<float> densities = onStack ? stackalloc float[count] : new float[count];
        sums.Clear();
        counts.Clear();
        mountedCounts.Clear();
        densities.Clear();
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

        for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            sums[groupIds[allyIndex]] += allies[allyIndex].Position;
            counts[groupIds[allyIndex]]++;
            if (allies[allyIndex].IsMounted)
            {
                mountedCounts[groupIds[allyIndex]]++;
            }
        }

        AccumulateDensities(allies, groupIds, densities);
        for (var label = 0; label < allies.Count; label++)
        {
            if (counts[label] > 0)
            {
                clusters.Add(new AllyCluster(sums[label] / counts[label], counts[label], mountedCounts[label],
                    DensestPoint(allies, groupIds, densities, label)));
            }
        }
        return clusters;
    }

    private static void AccumulateDensities(IReadOnlyList<PvpActor> allies, ReadOnlySpan<int> groupIds, Span<float> densities)
    {
        for (var firstIndex = 0; firstIndex < allies.Count; firstIndex++)
        {
            densities[firstIndex] += 1f;
            for (var secondIndex = firstIndex + 1; secondIndex < allies.Count; secondIndex++)
            {
                if (groupIds[firstIndex] != groupIds[secondIndex])
                {
                    continue;
                }

                var weight = 1f - Vector3.Distance(allies[firstIndex].Position, allies[secondIndex].Position) / DensityKernelYalms;
                if (weight > 0f)
                {
                    densities[firstIndex] += weight;
                    densities[secondIndex] += weight;
                }
            }
        }
    }

    private static Vector3 DensestPoint(IReadOnlyList<PvpActor> allies, ReadOnlySpan<int> groupIds, ReadOnlySpan<float> densities, int label)
    {
        var peakIndex = -1;
        for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            if (groupIds[allyIndex] == label && (peakIndex < 0 || densities[allyIndex] > densities[peakIndex]))
            {
                peakIndex = allyIndex;
            }
        }

        var peak = allies[peakIndex].Position;
        var sum = Vector3.Zero;
        var members = 0;
        for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            if (groupIds[allyIndex] == label && Vector3.Distance(allies[allyIndex].Position, peak) <= DensestCoreYalms)
            {
                sum += allies[allyIndex].Position;
                members++;
            }
        }
        return sum / members;
    }

    public static AllyCluster? Largest(List<AllyCluster> clusters, Vector3 self)
    {
        AllyCluster? best = null;
        var bestDistance = float.MaxValue;
        for (var clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
        {
            var cluster = clusters[clusterIndex];
            var distance = Vector3.Distance(self, cluster.Centroid);
            if (best is not { } current
                || cluster.Size > current.Size
                || (cluster.Size == current.Size && distance < bestDistance))
            {
                best = cluster;
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
