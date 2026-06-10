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
    private const float AllyBlendMinDot = -0.15f; // below this the team is behind the enemy line; flee to spawn instead
    private const float FocusFalloffMult = 2f;    // a focuser past ThreatRadius*this contributes no pressure
    private const float FocusFalloffEpsilon = 0.01f;
    private const float EnemyBaseAvoidRadius = 25f; // defensive moves never resolve this close to the enemy spawn gate
    private const float MeleeFocusBump = 0.5f;    // a melee in your face commits harder than a ranged poke
    private const long BurstWindowMs = 1000;      // hp-drop rate is measured over this window, not tick-to-tick
    private const long MinDwellMs = 700;          // hold a defensive stance this long before relaxing it (anti-thrash)
    private const float KillPotentialWeight = 3f;
    private const float FocusFireVoteWeight = 1.5f;
    private const float TargetDistanceWeight = 1.5f;
    private const float GuardPenalty = 4f;
    private const float StickyTargetBonus = 0.75f;
    private const float HealerPriorityBonus = 1.5f;
    private const float RangedPriorityBonus = 1f;
    private const float CrystalRideStopRange = 1f;
    private const float CrystalRideHeadStart = 8f;   // ride a free point only with this much travel lead over the nearest enemy
    private const float CrystalRideEscortSlack = 8f; // or with the ally cluster arriving within this many yalms of you
    private const int StagePointDeficit = 2;          // allies on the point outmanned by this many = stage, don't trickle in
    private const float MinFanRadius = 1f;            // closer than this the blocker is vertical; swinging sideways won't help
    private const int EscapeCandidateCount = 12;
    private const float EscapeThreatScale = 30f;      // yalm-equivalent pressure of one enemy at point-blank
    private const float MinThreatDistance = 2f;
    private const float EscapeAnchorWeight = 0.5f;    // each yalm closer to the pull point is worth this much
    private const float EscapeLosBreakBonus = 8f;     // breaking the nearest focuser's sight line ~= 16y closer to the team
    private const float ThreatFocuserBump = 0.75f;
    private const float ThreatMeleeBump = 0.5f;
    private const float TeamAdvantageForceWeight = 0.5f; // each net dead enemy counts as half a fighter on our side
    private const float DegToRad = MathF.PI / 180f;

    private static readonly float[] FanAngleDegrees = [30f, -30f, 60f, -60f, 90f, -90f, 120f, -120f];

    // Ordered by urgency; ApplyDwell compares ranks via (int), so the order matters.
    private enum Stance { Engage, Stage, Reposition, Regroup, Retreat }

    private readonly record struct EngageChoice(
        Vector3 Destination,
        float StopRange,
        bool Pursue,
        bool Sprint,
        string Label,
        Posture Posture);

    private readonly HashSet<ulong> allyRoster = [];
    private readonly HashSet<ulong> enemyRoster = [];
    private StrategyProfile baseProfile = StrategyProfile.For(strategy);
    private StrategyProfile profile = StrategyProfile.For(strategy);
    private bool roleOverlayEnabled = strategy != PvpStrategy.Custom;
    private PvpRole appliedRole = PvpRole.Unknown;
    private readonly Queue<(long Tick, float Hp)> hpSamples = new();
    private Vector3? enemyBase;
    private bool retreating;
    private Stance committedStance = Stance.Engage;
    private long committedAtMs;
    private ulong lastTargetId;

    public bool OwnsTargeting { get; set; }

    public Func<Vector3, Vector3, bool> CanSee { get; set; } = static (_, _) => true;

    public void SetStrategy(PvpStrategy s, CustomStrategyProfile? custom = null)
    {
        baseProfile = StrategyProfile.For(s, custom);
        roleOverlayEnabled = s != PvpStrategy.Custom;
        profile = EffectiveProfile(appliedRole);
    }

    private void ApplyRole(PvpRole role)
    {
        if (role == appliedRole)
        {
            return;
        }
        appliedRole = role;
        profile = EffectiveProfile(role);
    }

    private StrategyProfile EffectiveProfile(PvpRole role)
        => roleOverlayEnabled ? baseProfile.WithRole(role) : baseProfile;

    public void Reset()
    {
        allyRoster.Clear();
        enemyRoster.Clear();
        hpSamples.Clear();
        enemyBase = null;
        retreating = false;
        committedStance = Stance.Engage;
        committedAtMs = 0;
        lastTargetId = 0;
    }

    public MovePlan Decide(PvpSnapshot snapshot, Vector3 safeAnchor, Vector3? enemyBasePosition = null)
    {
        enemyBase = enemyBasePosition;
        ApplyRole(snapshot.SelfRole);
        var bursting = HpDropPerSec(snapshot.SelfHp) >= profile.BurstDropPerSec;

        var focal = FocalPoint(snapshot);

        var enemiesNear = PvpSnapshot.CountWithin(snapshot.Enemies, snapshot.Self, profile.ThreatRadius);
        var alliesNear = PvpSnapshot.CountWithin(snapshot.Allies, snapshot.Self, profile.SupportRadius);
        var headcountForce = (1 + alliesNear) - enemiesNear;
        var teamAdvantage = TeamAdvantage(snapshot);
        var effectiveForce = WeightedForce(snapshot) + teamAdvantage * TeamAdvantageForceWeight;
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
            else if (snapshot.SelfHp <= profile.DisengageHp && (focus >= profile.FocusRetreatCount || headcountForce < 0 || bursting))
            {
                retreating = true;
            }
        }
        else if (snapshot.SelfHp >= profile.ReengageHp && headcountForce >= 0 && focus < profile.FocusRetreatCount)
        {
            retreating = false;
        }

        var desired = ChooseStance(effectiveForce, isolated, enemiesNear, enemiesAtPoint, alliesAtPoint, focus, bursting,
            snapshot.AllyCluster is not null);
        var stance = ApplyDwell(desired);

        var advantageTag = TeamAdvantageTag(teamAdvantage);
        return stance switch
        {
            Stance.Retreat => FallBack(snapshot, safeAnchor, Posture.Retreat, $"retreat hp={snapshot.SelfHp:P0} focus={snapshot.FocusCount}"),
            Stance.Regroup => FallBack(snapshot, safeAnchor, Posture.Regroup, $"regroup {1 + alliesNear}v{enemiesNear} on you{advantageTag}"),
            Stance.Reposition => Reposition(snapshot, bursting, $"focused x{snapshot.FocusCount}, reposition"),
            Stance.Stage => Stage(snapshot, focal, $"staging {1 + alliesNear}v{enemiesNear}, wait for team{advantageTag}"),
            _ => Engage(snapshot, focal, alliesNear, enemiesNear, effectiveForce, advantageTag),
        };
    }

    private float WeightedForce(PvpSnapshot snapshot)
    {
        var force = 1f;
        for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
        {
            var ally = snapshot.Allies[allyIndex];
            if (ally.DistanceToSelf <= profile.SupportRadius)
            {
                force += ally.Hp;
            }
        }
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (enemy.DistanceToSelf <= profile.ThreatRadius)
            {
                force -= enemy.Hp;
            }
        }
        return force;
    }

    private int TeamAdvantage(PvpSnapshot snapshot)
    {
        for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
        {
            allyRoster.Add(snapshot.Allies[allyIndex].Id);
        }
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            enemyRoster.Add(snapshot.Enemies[enemyIndex].Id);
        }
        var deadEnemies = Math.Max(0, enemyRoster.Count - snapshot.Enemies.Count);
        var deadAllies = Math.Max(0, allyRoster.Count - snapshot.Allies.Count);
        return deadEnemies - deadAllies;
    }

    private static string TeamAdvantageTag(int teamAdvantage) => teamAdvantage switch
    {
        > 0 => $", {teamAdvantage} up",
        < 0 => $", {-teamAdvantage} down",
        _ => "",
    };

    private Stance ChooseStance(float effectiveForce, bool isolated, int enemiesNear, int enemiesAtPoint, int alliesAtPoint,
        float focus, bool bursting, bool hasTeam)
    {
        if (retreating)
            return Stance.Retreat;

        var soloFeed = isolated && enemiesNear >= SoloFeedEnemies;
        var overwhelmed = enemiesNear >= 1 && effectiveForce < -profile.OutnumberMargin;
        if (soloFeed || overwhelmed)
            return Stance.Regroup;

        if (focus >= profile.FocusRepositionCount && (effectiveForce <= 0f || isolated || bursting))
            return Stance.Reposition;

        var pointHeldByEnemy = alliesAtPoint == 0 && enemiesAtPoint > 0;
        var trickleFight = alliesAtPoint > 0 && enemiesAtPoint - alliesAtPoint >= StagePointDeficit;
        if (hasTeam && (pointHeldByEnemy || trickleFight) && effectiveForce < 0f)
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

    private static Vector3 FocalPoint(PvpSnapshot snapshot)
        => snapshot.SelfRole == PvpRole.Healer
            ? snapshot.AllyCluster?.Centroid ?? snapshot.Objective ?? snapshot.Self
            : snapshot.Objective ?? snapshot.AllyCluster?.Centroid ?? snapshot.Self;

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

    private MovePlan Engage(PvpSnapshot snapshot, Vector3 focal, int alliesNear, int enemiesNear, float effectiveForce, string advantageTag)
    {
        var push = effectiveForce >= profile.PushAdvantage;
        var target = SelectTarget(snapshot, focal);

        if (RideableObjective(snapshot) is { } crystal)
        {
            return new MovePlan(
                Kind: MoveKind.Engage,
                Destination: crystal,
                Fallback: crystal,
                StopRange: CrystalRideStopRange,
                Sprint: true,
                Reason: "ride crystal, point free",
                Pursue: false,
                Posture: Posture.Push,
                TargetId: target?.Id ?? 0);
        }

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

        var destination = ClampCohesion(choice.Destination, snapshot.AllyCluster?.Centroid, profile.CohesionRadius);
        if (snapshot.PrefersBackline && target is { } losTarget)
        {
            destination = EnsureLineOfFire(losTarget, destination);
        }

        var targetDescription = target is { } chosenTarget ? $" → {(int)(chosenTarget.Hp * 100)}%@{chosenTarget.DistanceToSelf:F0}y" : "";
        var kind = choice.Posture == Posture.Hold ? MoveKind.Hold : MoveKind.Engage;
        return new MovePlan(
            Kind: kind,
            Destination: destination,
            Fallback: destination,
            StopRange: choice.StopRange,
            Sprint: choice.Sprint,
            Reason: $"{choice.Label} {1 + alliesNear}v{enemiesNear}{advantageTag}{targetDescription}",
            Pursue: choice.Pursue,
            Posture: choice.Posture,
            TargetId: target?.Id ?? 0);
    }

    private Vector3? RideableObjective(PvpSnapshot snapshot)
    {
        if (snapshot.Objective is not { } objective
            || PvpSnapshot.CountWithin(snapshot.Enemies, objective, profile.EngageRadius) > 0)
        {
            return null;
        }

        var selfDistance = Vector3.Distance(snapshot.Self, objective);
        var enemyDistance = float.MaxValue;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            enemyDistance = MathF.Min(enemyDistance, Vector3.Distance(snapshot.Enemies[enemyIndex].Position, objective));
        }
        if (enemyDistance >= selfDistance + CrystalRideHeadStart)
        {
            return objective;
        }

        if (snapshot.AllyCluster is { } cluster
            && Vector3.Distance(cluster.Centroid, objective) <= selfDistance + CrystalRideEscortSlack)
        {
            return objective;
        }
        return null;
    }

    private Vector3 EnsureLineOfFire(PvpActor target, Vector3 destination)
    {
        if (CanSee(destination, target.Position))
        {
            return destination;
        }

        var offset = destination - target.Position;
        offset.Y = 0;
        var radius = offset.Length();
        if (radius < MinFanRadius)
        {
            return destination;
        }

        var baseDirection = offset / radius;
        for (var angleIndex = 0; angleIndex < FanAngleDegrees.Length; angleIndex++)
        {
            var candidate = target.Position + RotateY(baseDirection, FanAngleDegrees[angleIndex] * DegToRad) * radius;
            if (CanSee(candidate, target.Position))
            {
                return candidate;
            }
        }
        return destination;
    }

    private static Vector3 RotateY(Vector3 v, float radians)
    {
        var sin = MathF.Sin(radians);
        var cos = MathF.Cos(radians);
        return new Vector3(v.X * cos - v.Z * sin, v.Y, v.X * sin + v.Z * cos);
    }

    private PvpActor? SelectTarget(PvpSnapshot snapshot, Vector3 focal)
    {
        if (OwnsTargeting || snapshot.PrefersBackline)
        {
            return ChooseTarget(snapshot, focal);
        }
        return snapshot.CurrentTarget ?? ChooseTarget(snapshot, focal);
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
        if (threat is not { } threatPosition || NormalizedAwayFrom(snapshot.Self, threatPosition) is not { } direction)
        {
            return new MovePlan(Kind: MoveKind.Retreat, Destination: safeAnchor, Fallback: safeAnchor,
                StopRange: RetreatStopRange, Sprint: true, Reason: reason, Pursue: false, Posture: posture);
        }

        var pull = safeAnchor;
        if (snapshot.AllyCluster?.Centroid is { } clusterCentroid)
        {
            var toAllies = clusterCentroid - snapshot.Self;
            if (toAllies.LengthSquared() > MinVectorSq)
            {
                if (Vector3.Dot(Vector3.Normalize(toAllies), direction) <= AllyBlendMinDot)
                {
                    return new MovePlan(Kind: MoveKind.Retreat, Destination: safeAnchor, Fallback: safeAnchor,
                        StopRange: RetreatStopRange, Sprint: true, Reason: reason, Pursue: false, Posture: posture);
                }
                pull = clusterCentroid;
            }
        }

        var kiteDestination = PickEscapePoint(snapshot, profile.KiteDistance, pull)
            ?? AwayFromEnemyBase(snapshot.Self + direction * profile.KiteDistance, snapshot.Self);
        return new MovePlan(Kind: MoveKind.Retreat, Destination: kiteDestination, Fallback: safeAnchor,
            StopRange: RetreatStopRange, Sprint: true, Reason: reason, Pursue: false, Posture: posture);
    }

    private MovePlan Reposition(PvpSnapshot snapshot, bool sprint, string reason)
    {
        var pull = snapshot.AllyCluster?.Centroid ?? snapshot.Self;
        var destination = PickEscapePoint(snapshot, profile.RepositionDistance, pull)
            ?? AwayFromEnemyBase(pull, snapshot.Self);
        destination = ClampCohesion(destination, snapshot.AllyCluster?.Centroid, profile.CohesionRadius);
        return new MovePlan(Kind: MoveKind.Retreat, Destination: destination, Fallback: destination,
            StopRange: RepositionStopRange, Sprint: sprint, Reason: reason, Pursue: false, Posture: Posture.Reposition);
    }

    private Vector3? PickEscapePoint(PvpSnapshot snapshot, float distance, Vector3 pull)
    {
        var losReference = NearestFocuserPos(snapshot) ?? NearestEnemyPos(snapshot);
        Vector3? best = null;
        var bestScore = float.MinValue;
        for (var candidateIndex = 0; candidateIndex < EscapeCandidateCount; candidateIndex++)
        {
            var angle = candidateIndex * (MathF.Tau / EscapeCandidateCount);
            var candidate = snapshot.Self + new Vector3(MathF.Cos(angle), 0f, MathF.Sin(angle)) * distance;
            if (TooCloseToEnemyBase(candidate))
            {
                continue;
            }
            var score = ScoreEscape(snapshot, candidate, pull, losReference);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }

    private float ScoreEscape(PvpSnapshot snapshot, Vector3 candidate, Vector3 pull, Vector3? losReference)
    {
        var score = -Vector3.Distance(candidate, pull) * EscapeAnchorWeight;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            var weight = 1f
                + (enemy.TargetId == snapshot.SelfId ? ThreatFocuserBump : 0f)
                + (enemy.IsMelee ? ThreatMeleeBump : 0f);
            score -= weight * EscapeThreatScale / MathF.Max(MinThreatDistance, Vector3.Distance(candidate, enemy.Position));
        }
        if (losReference is { } focuser && !CanSee(focuser, candidate))
        {
            score += EscapeLosBreakBonus;
        }
        return score;
    }

    private bool TooCloseToEnemyBase(Vector3 point)
        => enemyBase is { } basePosition && Vector3.Distance(point, basePosition) < EnemyBaseAvoidRadius;

    private MovePlan Stage(PvpSnapshot s, Vector3 focal, string reason)
    {
        var anchor = s.AllyCluster?.Centroid ?? s.Self;
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

        dest = AwayFromEnemyBase(ClampCohesion(dest, s.AllyCluster?.Centroid, profile.CohesionRadius), s.Self);
        return new MovePlan(MoveKind.Hold, dest, anchor, StageStopRange, false, reason, false, Posture.Stage);
    }

    private PvpActor? ChooseTarget(PvpSnapshot snapshot, Vector3 focal)
    {
        var pool = TargetPool(snapshot, focal);
        if (pool.Count == 0)
        {
            lastTargetId = 0;
            return null;
        }

        var best = pool[0];
        var bestScore = ScoreTarget(snapshot, best);
        for (var poolIndex = 1; poolIndex < pool.Count; poolIndex++)
        {
            var candidate = pool[poolIndex];
            var score = ScoreTarget(snapshot, candidate);
            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }
        lastTargetId = best.Id;
        return best;
    }

    private List<PvpActor> TargetPool(PvpSnapshot snapshot, Vector3 focal)
    {
        var pool = new List<PvpActor>();
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (Vector3.Distance(enemy.Position, focal) <= profile.LeashRadius)
            {
                pool.Add(enemy);
            }
        }
        if (pool.Count == 0 && snapshot.Enemies.Count > 0)
        {
            pool.Add(NearestEnemy(snapshot));
        }
        return pool;
    }

    private float ScoreTarget(PvpSnapshot snapshot, PvpActor enemy)
    {
        var score = (1f - enemy.Hp) * KillPotentialWeight;
        score += AllyVotes(snapshot, enemy.Id) * FocusFireVoteWeight;
        score += RolePriority(enemy.Role);
        score -= enemy.DistanceToSelf / profile.EngageRadius * TargetDistanceWeight;
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

    private static float RolePriority(PvpRole role) => role switch
    {
        PvpRole.Healer => HealerPriorityBonus,
        PvpRole.Ranged => RangedPriorityBonus,
        _ => 0f,
    };

    private Vector3 BacklineOnTarget(PvpSnapshot s, PvpActor target)
    {
        var anchor = s.AllyCluster?.Centroid ?? s.Self;
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

    // Peak-to-current hp loss over the trailing window; a single hit between two 150ms ticks
    // would otherwise read as an enormous instantaneous rate and trip every preset's threshold.
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
