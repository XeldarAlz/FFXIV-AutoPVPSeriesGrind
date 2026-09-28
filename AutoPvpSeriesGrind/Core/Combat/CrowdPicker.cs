using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class CrowdPicker
{
    private const float BaseRadiusYalms = 40f;
    private const int MinFieldGroupSize = 3;
    private const float SameGroupYalms = 20f;
    private const int SwitchConfirmMs = 2000;

    private readonly List<PvpActor> awayFromBase = new();
    private Vector3? ownBase;
    private long switchPendingSinceMs;

    public void Reset()
    {
        ownBase = null;
        switchPendingSinceMs = 0;
        awayFromBase.Clear();
    }

    public void MarkBase(Vector3 position) => ownBase = position;

    public AllyCluster? Pick(PvpSnapshot snapshot, Vector3? followed)
    {
        var clusters = FieldClusters(snapshot, out var preferred);
        if (preferred is not { } candidate)
        {
            switchPendingSinceMs = 0;
            return null;
        }
        if (followed is not { } followedCentroid
            || Nearest(clusters, followedCentroid) is not { } kept
            || kept.Centroid == candidate.Centroid)
        {
            switchPendingSinceMs = 0;
            return candidate;
        }

        var now = Environment.TickCount64;
        if (switchPendingSinceMs == 0)
        {
            switchPendingSinceMs = now;
        }
        if (now - switchPendingSinceMs < SwitchConfirmMs)
        {
            return kept;
        }

        switchPendingSinceMs = 0;
        return candidate;
    }

    private List<AllyCluster> FieldClusters(PvpSnapshot snapshot, out AllyCluster? preferred)
    {
        if (ownBase is { } basePosition)
        {
            awayFromBase.Clear();
            for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
            {
                var ally = snapshot.Allies[allyIndex];
                if (Vector3.Distance(ally.Position, basePosition) > BaseRadiusYalms)
                {
                    awayFromBase.Add(ally);
                }
            }

            var field = PvpSnapshot.Clusters(awayFromBase);
            preferred = PvpSnapshot.Largest(field, snapshot.Self);
            if (preferred is { Size: >= MinFieldGroupSize })
            {
                return field;
            }
        }

        var everyone = PvpSnapshot.Clusters(snapshot.Allies);
        preferred = PvpSnapshot.Largest(everyone, snapshot.Self);
        return everyone;
    }

    private static AllyCluster? Nearest(List<AllyCluster> clusters, Vector3 point)
    {
        AllyCluster? nearest = null;
        var nearestDistance = SameGroupYalms;
        for (var clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
        {
            var distance = Vector3.Distance(clusters[clusterIndex].Centroid, point);
            if (distance <= nearestDistance)
            {
                nearest = clusters[clusterIndex];
                nearestDistance = distance;
            }
        }
        return nearest;
    }
}
