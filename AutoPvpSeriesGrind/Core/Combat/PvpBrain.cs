using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal enum MoveKind { Hold, Engage, Retreat }

internal readonly record struct MovePlan(MoveKind Kind, Vector3 Destination, Vector3 Fallback, float StopRange, bool Sprint, string Reason, bool Pursue = false);

internal sealed class PvpBrain(PvpStrategy strategy)
{
    private const float RetreatStopRange = 3f;
    private const float RegroupStopRange = 3f;
    private const float BacklineStopRange = 2f;
    private const float RegroupStepBack = 10f;   // yalms off the point toward safety when no allies remain
    private const float MinVectorSq = 0.01f;     // below this a direction vector is treated as degenerate
    private const float PursuitSprintGap = 8f;

    private StrategyProfile profile = StrategyProfile.For(strategy);
    private bool retreating;

    public void SetStrategy(PvpStrategy s, CustomStrategyProfile? custom = null) => profile = StrategyProfile.For(s, custom);

    public void Reset() => retreating = false;

    public MovePlan Decide(PvpSnapshot s, Vector3 safeAnchor)
    {
        var focal = s.Objective ?? s.AllyCentroid ?? s.Self;
        var localEnemies = CountWithin(s.Enemies, focal, profile.EngageRadius);
        var localAllies = 1 + CountWithin(s.Allies, focal, profile.EngageRadius);
        var advantage = localAllies - localEnemies;
        var pressured = s.FocusCount >= profile.FocusRetreatCount;

        if (!retreating)
        {
            if (s.SelfHp <= profile.PanicHp || (s.SelfHp <= profile.DisengageHp && (pressured || advantage < 0)))
                retreating = true;
        }
        else if (s.SelfHp >= profile.ReengageHp)
        {
            retreating = false;
        }

        if (retreating)
            return Retreat(s, safeAnchor);

        if (advantage < -profile.OutnumberMargin && localEnemies > 0)
        {
            var regroup = ClampCohesion(Regroup(s, focal, safeAnchor), s.AllyCentroid, profile.CohesionRadius);
            return new MovePlan(MoveKind.Engage, regroup, safeAnchor, RegroupStopRange, false, $"regroup {localAllies}v{localEnemies}");
        }

        var push = advantage >= profile.PushAdvantage;
        var target = s.PrefersBackline ? ChooseTarget(s, focal) : (s.CurrentTarget ?? ChooseTarget(s, focal));

        Vector3 dest;
        float stop;
        var pursue = false;
        var sprint = false;
        string label;

        if (s.PrefersBackline)
        {
            dest = push && target is { } bt ? BacklineOnTarget(s, bt) : BacklineHold(s, focal);
            stop = BacklineStopRange;
            label = push ? "push" : "hold";
        }
        else if (target is { } mt)
        {
            dest = mt.Position;
            stop = profile.MeleeReach;
            pursue = true;
            sprint = mt.DistanceToSelf > profile.MeleeReach + PursuitSprintGap;
            label = "chase";
        }
        else
        {
            dest = focal;
            stop = profile.MeleeHoldRange;
            label = "hold";
        }

        dest = ClampCohesion(dest, s.AllyCentroid, profile.CohesionRadius);

        var tdesc = target is { } t ? $" → {(int)(t.Hp * 100)}%@{t.DistanceToSelf:F0}y" : "";
        return new MovePlan(MoveKind.Engage, dest, dest, stop, sprint, $"{label} {localAllies}v{localEnemies}{tdesc}", pursue);
    }

    private MovePlan Retreat(PvpSnapshot s, Vector3 safeAnchor)
    {
        var threat = NearestEnemyPos(s) ?? s.EnemyCentroid;
        if (threat is { } tp)
        {
            var away = s.Self - tp;
            if (away.LengthSquared() > MinVectorSq)
            {
                var dir = Vector3.Normalize(away);
                if (s.AllyCentroid is { } ac)
                {
                    var toAllies = ac - s.Self;
                    if (toAllies.LengthSquared() > MinVectorSq)
                        dir = Vector3.Normalize(dir + Vector3.Normalize(toAllies));
                }
                return new MovePlan(MoveKind.Retreat, s.Self + dir * profile.KiteDistance, safeAnchor, RetreatStopRange, true,
                    $"retreat hp={s.SelfHp:P0} focus={s.FocusCount}");
            }
        }
        return new MovePlan(MoveKind.Retreat, safeAnchor, safeAnchor, RetreatStopRange, true, $"retreat hp={s.SelfHp:P0}");
    }

    private PvpActor? ChooseTarget(PvpSnapshot s, Vector3 focal)
    {
        if (s.Enemies.Count == 0) return null;

        var pool = s.Enemies.Where(e => Vector3.Distance(e.Position, focal) <= profile.LeashRadius).ToList();
        if (pool.Count == 0) pool = s.Enemies.OrderBy(e => e.DistanceToSelf).Take(1).ToList();

        PvpActor? focus = null;
        var bestVotes = 0;
        foreach (var e in pool)
        {
            var votes = 0;
            foreach (var a in s.Allies)
                if (a.TargetId == e.Id) votes++;
            if (votes > bestVotes) { bestVotes = votes; focus = e; }
        }
        if (bestVotes >= 1 && focus is { } f) return f;

        return pool.OrderBy(e => e.Hp).ThenBy(e => e.DistanceToSelf).First();
    }

    private Vector3 BacklineOnTarget(PvpSnapshot s, PvpActor target)
    {
        var anchor = s.AllyCentroid ?? s.Self;
        var dir = anchor - target.Position;
        return dir.LengthSquared() > MinVectorSq ? target.Position + Vector3.Normalize(dir) * profile.RangedBand : target.Position;
    }

    private Vector3 BacklineHold(PvpSnapshot s, Vector3 focal)
    {
        if (s.EnemyCentroid is { } c)
        {
            var away = focal - c;
            if (away.LengthSquared() > MinVectorSq) return focal + Vector3.Normalize(away) * profile.RangedStandoff;
        }
        return focal;
    }

    private static Vector3 Regroup(PvpSnapshot s, Vector3 focal, Vector3 safeAnchor)
    {
        if (s.AllyCentroid is { } a) return a;
        var dir = safeAnchor - focal;
        return dir.LengthSquared() > MinVectorSq ? focal + Vector3.Normalize(dir) * RegroupStepBack : focal;
    }

    private static Vector3 ClampCohesion(Vector3 dest, Vector3? center, float radius)
    {
        if (center is not { } c) return dest;
        var v = dest - c;
        return v.LengthSquared() <= radius * radius ? dest : c + Vector3.Normalize(v) * radius;
    }

    private static int CountWithin(IReadOnlyList<PvpActor> actors, Vector3 center, float radius)
    {
        var n = 0;
        foreach (var a in actors)
            if (Vector3.Distance(a.Position, center) <= radius) n++;
        return n;
    }

    private static Vector3? NearestEnemyPos(PvpSnapshot s)
        => s.Enemies.Count == 0 ? null : s.Enemies.OrderBy(e => e.DistanceToSelf).First().Position;
}
