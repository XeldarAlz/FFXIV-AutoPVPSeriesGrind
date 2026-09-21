using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class FrontlineBrain
{
    private const float FrontOffsetYalms = 6f;
    private const float BackOffsetYalms = 6f;
    private const float RetreatOffsetYalms = 12f;
    private const float RegroupDistanceYalms = 25f;
    private const float MountDistanceYalms = 60f;
    private const float EnemyAwareYalms = 30f;
    private const float EngageRangeYalms = 25f;
    private const float EnemyFrontSampleYalms = 40f;
    private const float HurtHp = 0.45f;
    private const float FocusedHurtHp = 0.6f;
    private const float RecoveredHp = 0.7f;
    private const int FocusHurtCount = 2;
    private const float HoldStopRange = 2f;
    private const float TravelStopRange = 6f;
    private const float MinVectorSq = 0.01f;
    private const long BurstWindowMs = 1000;
    private const float BurstDropPerSec = 0.3f;
    private const float KillPotentialWeight = 3f;
    private const float FocusFireVoteWeight = 1.5f;
    private const float TargetDistanceWeight = 1.5f;
    private const float GuardPenalty = 4f;
    private const float StickyTargetBonus = 0.75f;
    private const float HealerPriorityBonus = 1.5f;
    private const float RangedPriorityBonus = 1f;

    private readonly Queue<(long Tick, float Hp)> hpSamples = new();
    private Vector3? lastKnownCrowd;
    private bool hurt;
    private ulong lastTargetId;

    public bool UnderBurst { get; private set; }
    public bool WantsMount { get; private set; }
    public bool WantsDismount { get; private set; }

    public void Reset()
    {
        hpSamples.Clear();
        lastKnownCrowd = null;
        hurt = false;
        lastTargetId = 0;
        UnderBurst = false;
        WantsMount = false;
        WantsDismount = false;
    }

    public MovePlan Decide(PvpSnapshot snapshot)
    {
        UnderBurst = HpDropPerSec(snapshot.SelfHp) >= BurstDropPerSec;
        UpdateHurt(snapshot);
        var target = ChooseTarget(snapshot);
        var targetId = target?.Id ?? 0;

        if (snapshot.AllyCluster is { } cluster)
        {
            lastKnownCrowd = cluster.Centroid;
        }
        if (lastKnownCrowd is not { } crowd)
        {
            WantsMount = false;
            WantsDismount = true;
            return new MovePlan(MoveKind.Hold, snapshot.Self, snapshot.Self, HoldStopRange, Sprint: false, "waiting for the team",
                Pursue: false, Posture.Hold, targetId);
        }

        var distanceToCrowd = Vector3.Distance(snapshot.Self, crowd);
        var enemiesNear = snapshot.NearestEnemyDistance <= EnemyAwareYalms;
        WantsMount = distanceToCrowd > MountDistanceYalms && !enemiesNear;
        WantsDismount = enemiesNear || distanceToCrowd <= RegroupDistanceYalms;

        if (distanceToCrowd > RegroupDistanceYalms)
        {
            return new MovePlan(MoveKind.Engage, crowd, crowd, TravelStopRange, Sprint: true, $"rejoin the team, {distanceToCrowd:F0}y away",
                Pursue: false, Posture.Regroup, targetId);
        }

        var front = FrontDirection(snapshot, crowd);
        if (hurt)
        {
            var safe = crowd - front * RetreatOffsetYalms;
            return new MovePlan(MoveKind.Retreat, safe, crowd, HoldStopRange, Sprint: true, $"hurt hp={snapshot.SelfHp:P0}, behind the team",
                Pursue: false, Posture.Retreat, targetId);
        }

        var station = crowd + front * (snapshot.PrefersBackline ? -BackOffsetYalms : FrontOffsetYalms);
        var fighting = target is { } chosen && snapshot.EnemiesWithin(EngageRangeYalms) > 0;
        var reason = fighting
            ? $"with the team → {(int)(target!.Value.Hp * 100)}%@{target.Value.DistanceToSelf:F0}y"
            : "with the team";
        return new MovePlan(MoveKind.Hold, station, crowd, HoldStopRange, Sprint: false, reason, Pursue: false, Posture.Hold, targetId);
    }

    private void UpdateHurt(PvpSnapshot snapshot)
    {
        if (!hurt)
        {
            hurt = snapshot.SelfHp <= HurtHp || (snapshot.FocusCount >= FocusHurtCount && snapshot.SelfHp <= FocusedHurtHp);
            return;
        }
        if (snapshot.SelfHp >= RecoveredHp && snapshot.FocusCount < FocusHurtCount)
        {
            hurt = false;
        }
    }

    private static Vector3 FrontDirection(PvpSnapshot snapshot, Vector3 crowd)
    {
        var sum = Vector3.Zero;
        var count = 0;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (Vector3.Distance(enemy.Position, crowd) <= EnemyFrontSampleYalms)
            {
                sum += enemy.Position;
                count++;
            }
        }
        if (count == 0)
        {
            return Vector3.Zero;
        }

        var toward = sum / count - crowd;
        toward.Y = 0f;
        return toward.LengthSquared() > MinVectorSq ? Vector3.Normalize(toward) : Vector3.Zero;
    }

    private PvpActor? ChooseTarget(PvpSnapshot snapshot)
    {
        PvpActor? best = null;
        var bestScore = float.MinValue;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.DistanceToSelf > EngageRangeYalms)
            {
                continue;
            }
            var score = ScoreTarget(snapshot, enemy);
            if (score > bestScore)
            {
                best = enemy;
                bestScore = score;
            }
        }
        lastTargetId = best?.Id ?? 0;
        return best;
    }

    private float ScoreTarget(PvpSnapshot snapshot, PvpActor enemy)
    {
        var score = (1f - enemy.Hp) * KillPotentialWeight;
        score += AllyVotes(snapshot, enemy.Id) * FocusFireVoteWeight;
        score += enemy.Role switch
        {
            PvpRole.Healer => HealerPriorityBonus,
            PvpRole.Ranged => RangedPriorityBonus,
            _ => 0f,
        };
        score -= enemy.DistanceToSelf / EngageRangeYalms * TargetDistanceWeight;
        if (enemy.HasGuard)
        {
            score -= GuardPenalty;
        }
        if (enemy.Id == lastTargetId)
        {
            score += StickyTargetBonus;
        }
        return score;
    }

    private static int AllyVotes(PvpSnapshot snapshot, ulong enemyId)
    {
        var votes = 0;
        for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
        {
            if (snapshot.Allies[allyIndex].TargetId == enemyId)
            {
                votes++;
            }
        }
        return votes;
    }

    private float HpDropPerSec(float hp)
    {
        var now = Environment.TickCount64;
        while (hpSamples.Count > 0 && now - hpSamples.Peek().Tick > BurstWindowMs)
        {
            hpSamples.Dequeue();
        }
        var peak = hp;
        foreach (var sample in hpSamples)
        {
            peak = MathF.Max(peak, sample.Hp);
        }
        hpSamples.Enqueue((now, hp));
        return (peak - hp) / (BurstWindowMs / 1000f);
    }
}
