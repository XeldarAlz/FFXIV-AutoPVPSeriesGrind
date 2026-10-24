using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Rotation;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal readonly record struct FireDecision(IBattleChara HardTarget, int EnemiesAffected);

internal sealed class FireDecisionMaker
{
    public const float ScanRangeYalms = 30f;

    private readonly List<IBattleChara> candidates = new(16);

    public FireDecision? Decide(in LbTargetingProfile profile, LbRule rule, LimitBreakOptions settings, IPlayerCharacter self,
        List<IBattleChara> hostiles, List<IBattleChara> allies, HpTracker tracker)
    {
        if (rule.Mode == LbFireMode.Offensive)
        {
            return PickTarget(in profile, rule, settings, self, hostiles, tracker);
        }

        var alliesCounted = rule.Mode == LbFireMode.Defensive ? CountHurtAllies(rule, self, allies) : CountWithin(self, allies, rule.AllyRadiusYalms);
        if (alliesCounted < rule.AllyCountNear || CountWithin(self, hostiles, rule.EnemyRadiusYalms) < rule.EnemyCountNear)
        {
            return null;
        }

        // A team-state rule says when to fire, but an action that only lands on an enemy (Encore of Light, Flare Star)
        // cannot be cast on the player, so it takes an enemy by its own reach with no HP threshold.
        return profile.RequiresEnemyTarget
            ? PickTarget(in profile, threshold: null, settings, self, hostiles, tracker)
            : new FireDecision(self, 0);
    }

    private static int CountHurtAllies(LbRule rule, IPlayerCharacter self, List<IBattleChara> allies)
    {
        var hurt = 0;
        for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            var ally = allies[allyIndex];
            if (VectorMath.HorizontalDistance(self.Position, ally.Position) <= rule.AllyRadiusYalms
                && HpMath.EffectiveHpPercent(ally) < rule.AllyHpPercent)
            {
                hurt++;
            }
        }

        return hurt;
    }

    private static int CountWithin(IPlayerCharacter self, List<IBattleChara> characters, float radiusYalms)
    {
        var count = 0;
        for (var characterIndex = 0; characterIndex < characters.Count; characterIndex++)
        {
            if (VectorMath.HorizontalDistance(self.Position, characters[characterIndex].Position) <= radiusYalms)
            {
                count++;
            }
        }

        return count;
    }

    private FireDecision? PickTarget(in LbTargetingProfile profile, LbRule? threshold, LimitBreakOptions settings, IPlayerCharacter self,
        List<IBattleChara> hostiles, HpTracker tracker)
    {
        candidates.Clear();
        for (var hostileIndex = 0; hostileIndex < hostiles.Count; hostileIndex++)
        {
            var hostile = hostiles[hostileIndex];
            if (IsWorthFiringOn(hostile, threshold, settings, tracker))
            {
                candidates.Add(hostile);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        if (profile.CentersOnCaster)
        {
            return DecidePointBlank(in profile, self);
        }

        return profile.Shape is LbCastShape.CircleAroundTarget or LbCastShape.GroundCircle
            ? DecideCircleAroundTarget(in profile, self)
            : DecideSingleTarget(in profile, self);
    }

    private static bool IsWorthFiringOn(IBattleChara hostile, LbRule? threshold, LimitBreakOptions settings, HpTracker tracker)
    {
        if (threshold is not null && !HpMath.IsBelowThreshold(hostile, threshold))
        {
            return false;
        }

        if (settings.SkipDoomedTargets && tracker.IsDoomed(hostile))
        {
            return false;
        }

        if (settings.SkipGuardedTargets && MatchState.HasStatus(hostile, PvpStatuses.Guard))
        {
            return false;
        }

        if (settings.SkipInvulnerableTargets && MatchState.HasAnyStatus(hostile, PvpStatuses.HardImmunities))
        {
            return false;
        }

        return settings.NameBlocklist.Count == 0 || !settings.IsBlocked(hostile.Name.TextValue);
    }

    private FireDecision? DecideSingleTarget(in LbTargetingProfile profile, IPlayerCharacter self)
    {
        var range = profile.CastRange(ScanRangeYalms);
        IBattleChara? pick = null;
        var bestHp = uint.MaxValue;
        for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            var candidate = candidates[candidateIndex];
            var hp = HpMath.EffectiveHp(candidate);
            if (hp < bestHp && VectorMath.HorizontalDistance(self.Position, candidate.Position) <= range)
            {
                pick = candidate;
                bestHp = hp;
            }
        }

        return pick is null ? null : new FireDecision(pick, 1);
    }

    private FireDecision? DecidePointBlank(in LbTargetingProfile profile, IPlayerCharacter self)
    {
        var radius = profile.AoeRadius;
        IBattleChara? pick = null;
        var bestHp = uint.MaxValue;
        var caught = 0;
        for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            var candidate = candidates[candidateIndex];
            if (VectorMath.HorizontalDistance(self.Position, candidate.Position) > radius)
            {
                continue;
            }

            caught++;
            var hp = HpMath.EffectiveHp(candidate);
            if (hp < bestHp)
            {
                pick = candidate;
                bestHp = hp;
            }
        }

        return pick is null ? null : new FireDecision(pick, caught);
    }

    private FireDecision? DecideCircleAroundTarget(in LbTargetingProfile profile, IPlayerCharacter self)
    {
        var castRange = profile.CastRange(ScanRangeYalms);
        var aoeRadius = profile.AoeRadius;
        IBattleChara? best = null;
        var bestScore = 0;
        var bestHp = uint.MaxValue;
        for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            var candidate = candidates[candidateIndex];
            if (VectorMath.HorizontalDistance(self.Position, candidate.Position) > castRange)
            {
                continue;
            }

            var score = 0;
            for (var neighbourIndex = 0; neighbourIndex < candidates.Count; neighbourIndex++)
            {
                if (VectorMath.HorizontalDistance(candidates[neighbourIndex].Position, candidate.Position) <= aoeRadius)
                {
                    score++;
                }
            }

            var hp = HpMath.EffectiveHp(candidate);
            if (score > bestScore || (score == bestScore && hp < bestHp))
            {
                best = candidate;
                bestScore = score;
                bestHp = hp;
            }
        }

        return best is null ? null : new FireDecision(best, bestScore);
    }
}
