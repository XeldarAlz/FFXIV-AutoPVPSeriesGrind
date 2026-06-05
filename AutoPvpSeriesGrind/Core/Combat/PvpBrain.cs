using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal enum MoveKind { Hold, Engage, Retreat }

// Posture is the high-level intent shown in the overlay; MoveKind is how the executor actually moves.
internal enum Posture { Idle, Hold, Push, Stage, Reposition, Regroup, Retreat }

internal readonly record struct MovePlan(
    MoveKind Kind,
    Vector3 Destination,
    Vector3 Fallback,
    float StopRange,
    bool Sprint,
    string Reason,
    bool Pursue = false,
    Posture Posture = Posture.Idle);

internal sealed class PvpBrain(PvpStrategy strategy)
{
    private const float RetreatStopRange = 3f;
    private const float StageStopRange = 2f;
    private const float RepositionStopRange = 1.5f;
    private const float BacklineStopRange = 2f;
    private const float StageLead = 5f;          // yalms ahead of the team toward the point while staging
    private const float MinVectorSq = 0.01f;     // below this a direction vector is treated as degenerate
    private const float PursuitSprintGap = 8f;
    private const int SoloFeedEnemies = 2;        // alone (no ally nearby) vs this many = a feed, never solo it
    private const float AllyBlendMinDot = -0.15f; // below this the team is behind the enemy line — flee to spawn instead
    private const float RepositionAwayWeight = 0.6f;
    private const float RepositionTeamWeight = 0.4f;
    private const float FocusFalloffMult = 2f;    // a focuser past ThreatRadius*this contributes no pressure
    private const float MeleeFocusBump = 0.5f;    // a melee in your face commits harder than a ranged poke
    private const long MinDwellMs = 700;          // hold a defensive stance this long before relaxing it (anti-thrash)

    // Ordered by urgency — ApplyDwell compares ranks via (int), so the order matters.
    private enum Stance { Engage, Stage, Reposition, Regroup, Retreat }

    private StrategyProfile profile = StrategyProfile.For(strategy);
    private bool retreating;
    private float lastHp = 1f;
    private long lastHpTick;
    private Stance committedStance = Stance.Engage;
    private long committedAtMs;

    public void SetStrategy(PvpStrategy s, CustomStrategyProfile? custom = null) => profile = StrategyProfile.For(s, custom);

    public void Reset()
    {
        retreating = false;
        lastHp = 1f;
        lastHpTick = 0;
        committedStance = Stance.Engage;
        committedAtMs = 0;
    }

    public MovePlan Decide(PvpSnapshot s, Vector3 safeAnchor)
    {
        var bursting = HpDropPerSec(s.SelfHp) >= profile.BurstDropPerSec;

        var focal = s.Objective ?? s.AllyCentroid ?? s.Self;

        var enemiesNear = CountWithin(s.Enemies, s.Self, profile.ThreatRadius);
        var alliesNear = CountWithin(s.Allies, s.Self, profile.SupportRadius);
        var localForce = (1 + alliesNear) - enemiesNear;
        var isolated = alliesNear == 0;

        var enemiesAtPoint = CountWithin(s.Enemies, focal, profile.EngageRadius);
        var alliesAtPoint = CountWithin(s.Allies, focal, profile.EngageRadius);

        var focus = WeightedFocus(s);

        if (!retreating)
        {
            if (s.SelfHp <= profile.PanicHp)
                retreating = true;
            else if (s.SelfHp <= profile.DisengageHp && (focus >= profile.FocusRetreatCount || localForce < 0 || bursting))
                retreating = true;
        }
        else if (s.SelfHp >= profile.ReengageHp && localForce >= 0 && focus < profile.FocusRetreatCount)
        {
            retreating = false;
        }

        var desired = ChooseStance(localForce, isolated, enemiesNear, enemiesAtPoint, alliesAtPoint, focus, bursting,
            s.AllyCentroid is not null);
        var stance = ApplyDwell(desired);

        return stance switch
        {
            Stance.Retreat => FallBack(s, safeAnchor, Posture.Retreat, $"retreat hp={s.SelfHp:P0} focus={s.FocusCount}"),
            Stance.Regroup => FallBack(s, safeAnchor, Posture.Regroup, $"regroup {1 + alliesNear}v{enemiesNear} on you"),
            Stance.Reposition => Reposition(s, bursting, $"focused x{s.FocusCount} — reposition"),
            Stance.Stage => Stage(s, focal, $"staging {1 + alliesNear}v{enemiesNear} — wait for team"),
            _ => Engage(s, focal, alliesNear, enemiesNear),
        };
    }

    private Stance ChooseStance(int localForce, bool isolated, int enemiesNear, int enemiesAtPoint, int alliesAtPoint,
        float focus, bool bursting, bool hasTeam)
    {
        if (retreating)
            return Stance.Retreat;

        var soloFeed = isolated && enemiesNear >= SoloFeedEnemies;
        var overwhelmed = enemiesNear >= 1 && localForce < -profile.OutnumberMargin;
        if (soloFeed || overwhelmed)
            return Stance.Regroup;

        if (focus >= profile.FocusRepositionCount && (localForce <= 0 || isolated || bursting))
            return Stance.Reposition;

        if (hasTeam && enemiesAtPoint > 0 && alliesAtPoint == 0 && localForce < 0)
            return Stance.Stage;

        return Stance.Engage;
    }

    // Escalation is immediate; relaxing out of a defensive stance waits MinDwellMs so it can't thrash at a boundary.
    private Stance ApplyDwell(Stance desired)
    {
        var now = Environment.TickCount64;
        if (desired == committedStance)
            return committedStance;

        var escalating = (int)desired > (int)committedStance;
        if (!escalating && committedStance != Stance.Engage && now - committedAtMs < MinDwellMs)
            return committedStance;

        committedStance = desired;
        committedAtMs = now;
        return desired;
    }

    private float WeightedFocus(PvpSnapshot s)
    {
        var start = profile.ThreatRadius;
        var end = MathF.Max(start * FocusFalloffMult, start + 0.01f);
        var sum = 0f;
        foreach (var e in s.Enemies)
        {
            if (e.TargetId != s.SelfId) continue;
            var d = e.DistanceToSelf;
            var w = d <= start ? 1f : d >= end ? 0f : 1f - (d - start) / (end - start);
            if (e.IsMelee && d <= start) w += MeleeFocusBump;
            sum += w;
        }
        return sum;
    }

    private MovePlan Engage(PvpSnapshot s, Vector3 focal, int alliesNear, int enemiesNear)
    {
        var localForce = (1 + alliesNear) - enemiesNear;
        var push = localForce >= profile.PushAdvantage;
        var target = s.PrefersBackline ? ChooseTarget(s, focal) : (s.CurrentTarget ?? ChooseTarget(s, focal));

        Vector3 dest;
        float stop;
        var pursue = false;
        var sprint = false;
        string label;
        Posture posture;

        if (s.PrefersBackline)
        {
            dest = push && target is { } bt ? BacklineOnTarget(s, bt) : BacklineHold(s, focal);
            stop = BacklineStopRange;
            label = push ? "push" : "hold";
            posture = push ? Posture.Push : Posture.Hold;
        }
        else if (push && target is { } mt)
        {
            dest = mt.Position;
            stop = profile.MeleeReach;
            pursue = true;
            sprint = mt.DistanceToSelf > profile.MeleeReach + PursuitSprintGap;
            label = "chase";
            posture = Posture.Push;
        }
        else
        {
            dest = focal;
            stop = profile.MeleeHoldRange;
            label = "hold";
            posture = Posture.Hold;
        }

        dest = ClampCohesion(dest, s.AllyCentroid, profile.CohesionRadius);

        var tdesc = target is { } t ? $" → {(int)(t.Hp * 100)}%@{t.DistanceToSelf:F0}y" : "";
        var kind = posture == Posture.Hold ? MoveKind.Hold : MoveKind.Engage;
        return new MovePlan(kind, dest, dest, stop, sprint, $"{label} {1 + alliesNear}v{enemiesNear}{tdesc}", pursue, posture);
    }

    private MovePlan FallBack(PvpSnapshot s, Vector3 safeAnchor, Posture posture, string reason)
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
                    {
                        var an = Vector3.Normalize(toAllies);
                        if (Vector3.Dot(an, dir) > AllyBlendMinDot)
                            dir = Vector3.Normalize(dir + an);
                        else
                            return new MovePlan(MoveKind.Retreat, safeAnchor, safeAnchor, RetreatStopRange, true, reason, false, posture);
                    }
                }
                return new MovePlan(MoveKind.Retreat, s.Self + dir * profile.KiteDistance, safeAnchor, RetreatStopRange, true, reason, false, posture);
            }
        }
        return new MovePlan(MoveKind.Retreat, safeAnchor, safeAnchor, RetreatStopRange, true, reason, false, posture);
    }

    private MovePlan Reposition(PvpSnapshot s, bool sprint, string reason)
    {
        var dir = Vector3.Zero;
        if ((NearestFocuserPos(s) ?? NearestEnemyPos(s) ?? s.EnemyCentroid) is { } tp)
        {
            var away = s.Self - tp;
            if (away.LengthSquared() > MinVectorSq) dir = Vector3.Normalize(away);
        }
        if (s.AllyCentroid is { } ac)
        {
            var toAllies = ac - s.Self;
            if (toAllies.LengthSquared() > MinVectorSq)
            {
                var an = Vector3.Normalize(toAllies);
                dir = dir == Vector3.Zero ? an : Vector3.Normalize(dir * RepositionAwayWeight + an * RepositionTeamWeight);
            }
        }
        if (dir == Vector3.Zero) dir = Vector3.UnitX;

        var dest = ClampCohesion(s.Self + dir * profile.RepositionDistance, s.AllyCentroid, profile.CohesionRadius);
        return new MovePlan(MoveKind.Retreat, dest, dest, RepositionStopRange, sprint, reason, false, Posture.Reposition);
    }

    private MovePlan Stage(PvpSnapshot s, Vector3 focal, string reason)
    {
        var anchor = s.AllyCentroid ?? s.Self;
        var dest = anchor;

        var toPoint = focal - anchor;
        if (toPoint.LengthSquared() > MinVectorSq)
            dest = anchor + Vector3.Normalize(toPoint) * StageLead;

        if (s.EnemyCentroid is { } ec)
        {
            var fromEnemy = dest - ec;
            var d = fromEnemy.Length();
            if (d > MinVectorSq && d < profile.StageStandoff)
                dest = ec + Vector3.Normalize(fromEnemy) * profile.StageStandoff;
        }

        dest = ClampCohesion(dest, s.AllyCentroid, profile.CohesionRadius);
        return new MovePlan(MoveKind.Hold, dest, anchor, StageStopRange, false, reason, false, Posture.Stage);
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

    private float HpDropPerSec(float hp)
    {
        var now = Environment.TickCount64;
        var drop = 0f;
        if (lastHpTick != 0)
        {
            var dt = (now - lastHpTick) / 1000f;
            if (dt > 0.0001f) drop = (lastHp - hp) / dt;
        }
        lastHp = hp;
        lastHpTick = now;
        return drop;
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

    private static Vector3? NearestFocuserPos(PvpSnapshot s)
    {
        PvpActor? best = null;
        foreach (var e in s.Enemies)
            if (e.TargetId == s.SelfId && (best is null || e.DistanceToSelf < best.Value.DistanceToSelf))
                best = e;
        return best?.Position;
    }
}
