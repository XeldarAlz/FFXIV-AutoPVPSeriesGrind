using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal static class TargetScoring
{
    private const float KillPotentialWeight = 3f;
    private const float FocusFireVoteWeight = 1.5f;
    private const float TargetDistanceWeight = 1.5f;
    private const float GuardPenalty = 4f;
    private const float StickyTargetBonus = 0.75f;
    private const float HealerPriorityBonus = 1.5f;
    private const float RangedPriorityBonus = 1f;
    private const float ObjectiveVoteWeight = 2f;

    public static float Score(PvpSnapshot snapshot, in PvpActor enemy, float rangeYalms, ulong stickyId)
    {
        var score = (1f - enemy.Hp) * KillPotentialWeight;
        score += AllyVotes(snapshot, enemy.Id) * FocusFireVoteWeight;
        score += RolePriority(enemy.Role);
        score -= enemy.DistanceToSelf / rangeYalms * TargetDistanceWeight;
        if (enemy.HasGuard)
        {
            score -= GuardPenalty;
        }
        if (enemy.Id == stickyId)
        {
            score += StickyTargetBonus;
        }
        return score;
    }

    public static PvpActor? Best(PvpSnapshot snapshot, float rangeYalms, ulong stickyId)
    {
        PvpActor? best = null;
        var bestScore = float.MinValue;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.DistanceToSelf > rangeYalms)
            {
                continue;
            }

            var score = Score(snapshot, in enemy, rangeYalms, stickyId);
            if (score > bestScore)
            {
                best = enemy;
                bestScore = score;
            }
        }
        return best;
    }

    public static PvpActor? LowestHp(PvpSnapshot snapshot, float rangeYalms)
    {
        PvpActor? lowestOpen = null;
        PvpActor? lowestGuarded = null;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.DistanceToSelf > rangeYalms)
            {
                continue;
            }
            if (enemy.HasGuard)
            {
                if (lowestGuarded is null || enemy.Hp < lowestGuarded.Value.Hp)
                {
                    lowestGuarded = enemy;
                }
            }
            else if (lowestOpen is null || enemy.Hp < lowestOpen.Value.Hp)
            {
                lowestOpen = enemy;
            }
        }
        return lowestOpen ?? lowestGuarded ?? snapshot.NearestEnemy;
    }

    public static PvpStructure? TeamObjective(PvpSnapshot snapshot, Vector3 around, float reachYalms, float rangeYalms)
    {
        PvpStructure? best = null;
        var bestScore = float.MinValue;
        for (var structureIndex = 0; structureIndex < snapshot.Structures.Count; structureIndex++)
        {
            var structure = snapshot.Structures[structureIndex];
            var distance = VectorMath.HorizontalDistance(structure.Position, around);
            if (structure.AllyVotes == 0 || distance > structure.Radius + reachYalms)
            {
                continue;
            }

            var score = structure.AllyVotes * ObjectiveVoteWeight - distance / rangeYalms;
            if (score > bestScore)
            {
                best = structure;
                bestScore = score;
            }
        }
        return best;
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

    private static float RolePriority(PvpRole role) => role switch
    {
        PvpRole.Healer => HealerPriorityBonus,
        PvpRole.Ranged => RangedPriorityBonus,
        _ => 0f,
    };
}
