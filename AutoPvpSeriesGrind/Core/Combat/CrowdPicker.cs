using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class CrowdPicker
{
    private const float BaseRadiusYalms = 40f;
    private const int MinFieldGroupSize = 3;
    private const float SameGroupYalms = 20f;
    private const int SwitchConfirmMs = 2000;
    private const float YalmsPerAlly = 40f;
    private const float MinSwitchGain = 2f;
    private const float SwitchGainShare = 0.25f;

    private readonly List<PvpActor> awayFromBase = new();
    private Vector3? ownBase;
    private long switchPendingSinceMs;

    public void Reset()
    {
        ownBase = null;
        switchPendingSinceMs = 0;
        awayFromBase.Clear();
    }

    public Vector3? Base => ownBase;

    public void MarkBase(Vector3 position) => ownBase = position;

    public AllyCluster? Pick(PvpSnapshot snapshot, Vector3? followed, long nowMs)
    {
        var clusters = FieldClusters(snapshot, out var preferred);
        if (preferred is not { } candidate)
        {
            switchPendingSinceMs = 0;
            return null;
        }
        if (followed is not { } followedPosition
            || Nearest(clusters, followedPosition) is not { } kept
            || kept.Densest == candidate.Densest)
        {
            switchPendingSinceMs = 0;
            return candidate;
        }
        if (!WorthSwitching(snapshot.Self, kept, candidate))
        {
            switchPendingSinceMs = 0;
            return kept;
        }

        if (switchPendingSinceMs == 0)
        {
            switchPendingSinceMs = nowMs;
        }
        if (nowMs - switchPendingSinceMs < SwitchConfirmMs)
        {
            return kept;
        }

        switchPendingSinceMs = 0;
        return candidate;
    }

    private List<AllyCluster> FieldClusters(PvpSnapshot snapshot, out AllyCluster? preferred)
    {
        var allies = snapshot.RegroupAllies ?? snapshot.Allies;
        if (ownBase is { } basePosition)
        {
            awayFromBase.Clear();
            for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
            {
                var ally = allies[allyIndex];
                if (Vector3.Distance(ally.Position, basePosition) > BaseRadiusYalms)
                {
                    awayFromBase.Add(ally);
                }
            }

            var field = PvpSnapshot.Clusters(awayFromBase);
            var leftBase = Vector3.Distance(snapshot.Self, basePosition) > BaseRadiusYalms;
            if (leftBase || PvpSnapshot.Largest(field, snapshot.Self) is { Size: >= MinFieldGroupSize })
            {
                preferred = Best(field, snapshot.Self);
                return field;
            }
        }

        var everyone = PvpSnapshot.Clusters(allies);
        preferred = Best(everyone, snapshot.Self);
        return everyone;
    }

    private static float Score(AllyCluster cluster, Vector3 self)
        => cluster.Size - Vector3.Distance(self, cluster.Densest) / YalmsPerAlly;

    private static bool WorthSwitching(Vector3 self, AllyCluster kept, AllyCluster candidate)
        => Score(candidate, self) - Score(kept, self) >= MathF.Max(MinSwitchGain, kept.Size * SwitchGainShare);

    private static AllyCluster? Best(List<AllyCluster> clusters, Vector3 self)
    {
        AllyCluster? best = null;
        var bestScore = float.MinValue;
        for (var clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
        {
            var score = Score(clusters[clusterIndex], self);
            if (score > bestScore)
            {
                bestScore = score;
                best = clusters[clusterIndex];
            }
        }
        return best;
    }

    private static AllyCluster? Nearest(List<AllyCluster> clusters, Vector3 point)
    {
        AllyCluster? nearest = null;
        var nearestDistance = SameGroupYalms;
        for (var clusterIndex = 0; clusterIndex < clusters.Count; clusterIndex++)
        {
            var distance = Vector3.Distance(clusters[clusterIndex].Densest, point);
            if (distance <= nearestDistance)
            {
                nearest = clusters[clusterIndex];
                nearestDistance = distance;
            }
        }
        return nearest;
    }
}
