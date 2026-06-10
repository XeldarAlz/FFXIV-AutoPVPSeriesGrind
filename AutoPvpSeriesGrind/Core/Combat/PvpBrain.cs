using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

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
    private const float FocusFalloffEpsilon = 0.01f;
    private const float EnemyBaseAvoidRadius = 25f; // defensive moves never resolve this close to the enemy spawn gate
    private const float MeleeFocusBump = 0.5f;    // a melee in your face commits harder than a ranged poke
    private const float MinDeltaSeconds = 0.0001f;
    private const long MinDwellMs = 700;          // hold a defensive stance this long before relaxing it (anti-thrash)

    // Ordered by urgency — ApplyDwell compares ranks via (int), so the order matters.
    private enum Stance { Engage, Stage, Reposition, Regroup, Retreat }

    private readonly record struct EngageChoice(
        Vector3 Destination,
        float StopRange,
        bool Pursue,
        bool Sprint,
        string Label,
        Posture Posture);

    private StrategyProfile profile = StrategyProfile.For(strategy);
    private Vector3? enemyBase;
    private bool retreating;
    private float lastHp = 1f;
    private long lastHpTick;
    private Stance committedStance = Stance.Engage;
    private long committedAtMs;

    public void SetStrategy(PvpStrategy s, CustomStrategyProfile? custom = null) => profile = StrategyProfile.For(s, custom);

    public void Reset()
    {
        enemyBase = null;
        retreating = false;
        lastHp = 1f;
        lastHpTick = 0;
        committedStance = Stance.Engage;
        committedAtMs = 0;
    }

    public MovePlan Decide(PvpSnapshot snapshot, Vector3 safeAnchor, Vector3? enemyBasePosition = null)
    {
        enemyBase = enemyBasePosition;
        var bursting = HpDropPerSec(snapshot.SelfHp) >= profile.BurstDropPerSec;

        var focal = snapshot.Objective ?? snapshot.AllyCentroid ?? snapshot.Self;

        var enemiesNear = PvpSnapshot.CountWithin(snapshot.Enemies, snapshot.Self, profile.ThreatRadius);
        var alliesNear = PvpSnapshot.CountWithin(snapshot.Allies, snapshot.Self, profile.SupportRadius);
        var localForce = LocalForce(alliesNear, enemiesNear);
        var isolated = alliesNear == 0;

        var enemiesAtPoint = PvpSnapshot.CountWithin(snapshot.Enemies, focal, profile.EngageRadius);
        var alliesAtPoint = PvpSnapshot.CountWithin(snapshot.Allies, focal, profile.EngageRadius);

        var focus = WeightedFocus(snapshot);

        if (!retreating)
        {
            if (snapshot.SelfHp <= profile.PanicHp)
            {
                retreating = true;
            }
            else if (snapshot.SelfHp <= profile.DisengageHp && (focus >= profile.FocusRetreatCount || localForce < 0 || bursting))
            {
                retreating = true;
            }
        }
        else if (snapshot.SelfHp >= profile.ReengageHp && localForce >= 0 && focus < profile.FocusRetreatCount)
        {
            retreating = false;
        }

        var desired = ChooseStance(localForce, isolated, enemiesNear, enemiesAtPoint, alliesAtPoint, focus, bursting,
            snapshot.AllyCentroid is not null);
        var stance = ApplyDwell(desired);

        return stance switch
        {
            Stance.Retreat => FallBack(snapshot, safeAnchor, Posture.Retreat, $"retreat hp={snapshot.SelfHp:P0} focus={snapshot.FocusCount}"),
            Stance.Regroup => FallBack(snapshot, safeAnchor, Posture.Regroup, $"regroup {1 + alliesNear}v{enemiesNear} on you"),
            Stance.Reposition => Reposition(snapshot, bursting, $"focused x{snapshot.FocusCount} — reposition"),
            Stance.Stage => Stage(snapshot, focal, $"staging {1 + alliesNear}v{enemiesNear} — wait for team"),
            _ => Engage(snapshot, focal, alliesNear, enemiesNear),
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

    private static int LocalForce(int alliesNear, int enemiesNear) => (1 + alliesNear) - enemiesNear;

    private float WeightedFocus(PvpSnapshot snapshot)
    {
        var start = profile.ThreatRadius;
        var end = MathF.Max(start * FocusFalloffMult, start + FocusFalloffEpsilon);
        var sum = 0f;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.TargetId != snapshot.SelfId)
            {
                continue;
            }
            var distance = enemy.DistanceToSelf;
            var weight = distance <= start ? 1f : distance >= end ? 0f : 1f - (distance - start) / (end - start);
            if (enemy.IsMelee && distance <= start)
            {
                weight += MeleeFocusBump;
            }
            sum += weight;
        }
        return sum;
    }

    private MovePlan Engage(PvpSnapshot snapshot, Vector3 focal, int alliesNear, int enemiesNear)
    {
        var localForce = LocalForce(alliesNear, enemiesNear);
        var push = localForce >= profile.PushAdvantage;
        var target = snapshot.PrefersBackline ? ChooseTarget(snapshot, focal) : (snapshot.CurrentTarget ?? ChooseTarget(snapshot, focal));

        EngageChoice choice;
        if (snapshot.PrefersBackline)
        {
            choice = EngageBackline(snapshot, focal, push, target);
        }
        else if (push && target is { } meleeTarget)
        {
            choice = EngageChase(meleeTarget);
        }
        else
        {
            choice = EngageHold(focal);
        }

        var destination = ClampCohesion(choice.Destination, snapshot.AllyCentroid, profile.CohesionRadius);

        var targetDescription = target is { } chosenTarget ? $" → {(int)(chosenTarget.Hp * 100)}%@{chosenTarget.DistanceToSelf:F0}y" : "";
        var kind = choice.Posture == Posture.Hold ? MoveKind.Hold : MoveKind.Engage;
        return new MovePlan(
            Kind: kind,
            Destination: destination,
            Fallback: destination,
            StopRange: choice.StopRange,
            Sprint: choice.Sprint,
            Reason: $"{choice.Label} {1 + alliesNear}v{enemiesNear}{targetDescription}",
            Pursue: choice.Pursue,
            Posture: choice.Posture);
    }

    private EngageChoice EngageBackline(PvpSnapshot snapshot, Vector3 focal, bool push, PvpActor? target)
    {
        var destination = push && target is { } backlineTarget ? BacklineOnTarget(snapshot, backlineTarget) : BacklineHold(snapshot, focal);
        var label = push ? "push" : "hold";
        var posture = push ? Posture.Push : Posture.Hold;
        return new EngageChoice(destination, BacklineStopRange, Pursue: false, Sprint: false, label, posture);
    }

    private EngageChoice EngageChase(PvpActor target)
    {
        var sprint = target.DistanceToSelf > profile.MeleeReach + PursuitSprintGap;
        return new EngageChoice(target.Position, profile.MeleeReach, Pursue: true, sprint, "chase", Posture.Push);
    }

    private EngageChoice EngageHold(Vector3 focal)
        => new(focal, profile.MeleeHoldRange, Pursue: false, Sprint: false, "hold", Posture.Hold);

    private MovePlan FallBack(PvpSnapshot snapshot, Vector3 safeAnchor, Posture posture, string reason)
    {
        var threat = NearestEnemyPos(snapshot) ?? snapshot.EnemyCentroid;
        if (threat is { } threatPosition && NormalizedAwayFrom(snapshot.Self, threatPosition) is { } direction)
        {
            if (snapshot.AllyCentroid is { } allyCentroid)
            {
                var toAllies = allyCentroid - snapshot.Self;
                if (toAllies.LengthSquared() > MinVectorSq)
                {
                    var alliesDirection = Vector3.Normalize(toAllies);
                    if (Vector3.Dot(alliesDirection, direction) > AllyBlendMinDot)
                    {
                        direction = Vector3.Normalize(direction + alliesDirection);
                    }
                    else
                    {
                        return new MovePlan(Kind: MoveKind.Retreat, Destination: safeAnchor, Fallback: safeAnchor,
                            StopRange: RetreatStopRange, Sprint: true, Reason: reason, Pursue: false, Posture: posture);
                    }
                }
            }
            var kiteDestination = AwayFromEnemyBase(snapshot.Self + direction * profile.KiteDistance, snapshot.Self);
            return new MovePlan(Kind: MoveKind.Retreat, Destination: kiteDestination, Fallback: safeAnchor,
                StopRange: RetreatStopRange, Sprint: true, Reason: reason, Pursue: false, Posture: posture);
        }
        return new MovePlan(Kind: MoveKind.Retreat, Destination: safeAnchor, Fallback: safeAnchor,
            StopRange: RetreatStopRange, Sprint: true, Reason: reason, Pursue: false, Posture: posture);
    }

    private MovePlan Reposition(PvpSnapshot snapshot, bool sprint, string reason)
    {
        var direction = Vector3.Zero;
        if ((NearestFocuserPos(snapshot) ?? NearestEnemyPos(snapshot) ?? snapshot.EnemyCentroid) is { } threatPosition
            && NormalizedAwayFrom(snapshot.Self, threatPosition) is { } awayDirection)
        {
            direction = awayDirection;
        }
        if (snapshot.AllyCentroid is { } allyCentroid)
        {
            var toAllies = allyCentroid - snapshot.Self;
            if (toAllies.LengthSquared() > MinVectorSq)
            {
                var alliesDirection = Vector3.Normalize(toAllies);
                direction = direction == Vector3.Zero
                    ? alliesDirection
                    : Vector3.Normalize(direction * RepositionAwayWeight + alliesDirection * RepositionTeamWeight);
            }
        }
        if (direction == Vector3.Zero)
        {
            direction = Vector3.UnitX;
        }

        var destination = AwayFromEnemyBase(
            ClampCohesion(snapshot.Self + direction * profile.RepositionDistance, snapshot.AllyCentroid, profile.CohesionRadius),
            snapshot.Self);
        return new MovePlan(Kind: MoveKind.Retreat, Destination: destination, Fallback: destination,
            StopRange: RepositionStopRange, Sprint: sprint, Reason: reason, Pursue: false, Posture: Posture.Reposition);
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

        dest = AwayFromEnemyBase(ClampCohesion(dest, s.AllyCentroid, profile.CohesionRadius), s.Self);
        return new MovePlan(MoveKind.Hold, dest, anchor, StageStopRange, false, reason, false, Posture.Stage);
    }

    private PvpActor? ChooseTarget(PvpSnapshot snapshot, Vector3 focal)
    {
        if (snapshot.Enemies.Count == 0)
        {
            return null;
        }

        var pool = new List<PvpActor>();
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (Vector3.Distance(enemy.Position, focal) <= profile.LeashRadius)
            {
                pool.Add(enemy);
            }
        }
        if (pool.Count == 0)
        {
            pool.Add(NearestEnemy(snapshot));
        }

        PvpActor? focus = null;
        var bestVotes = 0;
        for (var poolIndex = 0; poolIndex < pool.Count; poolIndex++)
        {
            var enemy = pool[poolIndex];
            var votes = 0;
            for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
            {
                if (snapshot.Allies[allyIndex].TargetId == enemy.Id)
                {
                    votes++;
                }
            }
            if (votes > bestVotes)
            {
                bestVotes = votes;
                focus = enemy;
            }
        }
        if (bestVotes >= 1 && focus is { } focusTarget)
        {
            return focusTarget;
        }

        var best = pool[0];
        for (var poolIndex = 1; poolIndex < pool.Count; poolIndex++)
        {
            var candidate = pool[poolIndex];
            if (candidate.Hp < best.Hp || (candidate.Hp == best.Hp && candidate.DistanceToSelf < best.DistanceToSelf))
            {
                best = candidate;
            }
        }
        return best;
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
            var deltaSeconds = (now - lastHpTick) / 1000f;
            if (deltaSeconds > MinDeltaSeconds)
            {
                drop = (lastHp - hp) / deltaSeconds;
            }
        }
        lastHp = hp;
        lastHpTick = now;
        return drop;
    }

    private Vector3 AwayFromEnemyBase(Vector3 dest, Vector3 self)
    {
        if (enemyBase is not { } basePosition)
            return dest;
        var away = dest - basePosition;
        var distance = away.Length();
        if (distance >= EnemyBaseAvoidRadius)
            return dest;
        if (distance * distance <= MinVectorSq)
        {
            away = self - basePosition;
            distance = away.Length();
            if (distance * distance <= MinVectorSq)
                return dest;
        }
        return basePosition + away / distance * EnemyBaseAvoidRadius;
    }

    private static Vector3 ClampCohesion(Vector3 dest, Vector3? center, float radius)
    {
        if (center is not { } c) return dest;
        var v = dest - c;
        return v.LengthSquared() <= radius * radius ? dest : c + Vector3.Normalize(v) * radius;
    }

    private static Vector3? NormalizedAwayFrom(Vector3 self, Vector3 threat)
    {
        var away = self - threat;
        if (away.LengthSquared() <= MinVectorSq)
        {
            return null;
        }
        return Vector3.Normalize(away);
    }

    private static PvpActor NearestEnemy(PvpSnapshot snapshot)
    {
        var nearest = snapshot.Enemies[0];
        for (var enemyIndex = 1; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.DistanceToSelf < nearest.DistanceToSelf)
            {
                nearest = enemy;
            }
        }
        return nearest;
    }

    private static Vector3? NearestEnemyPos(PvpSnapshot snapshot)
        => snapshot.Enemies.Count == 0 ? null : NearestEnemy(snapshot).Position;

    private static Vector3? NearestFocuserPos(PvpSnapshot snapshot)
    {
        PvpActor? best = null;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.TargetId == snapshot.SelfId && (best is null || enemy.DistanceToSelf < best.Value.DistanceToSelf))
            {
                best = enemy;
            }
        }
        return best?.Position;
    }
}
