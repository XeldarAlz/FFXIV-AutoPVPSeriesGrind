using AutoPvpSeriesGrind.Core.Humanize;
using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal class FrontlineBrain
{
    private const float EnemyFrontSampleYalms = 40f;
    private const float HoldStopRange = 2f;
    private const float TravelStopRange = 6f;
    private const float CrowdLeadSeconds = 1.2f;

    private const float FormationLateralWander = 2.4f;
    private const float FormationDepthWander = 1.8f;
    private const float FormationDriftYalmsPerSec = 0.7f;
    private const int FormationHoldMinMs = 6000;
    private const int FormationHoldMaxMs = 15000;
    private const float AxisBlend = 0.25f;
    private const float AxisReversalCosine = -0.9f;
    private const float AllySeparationYalms = 2.4f;

    private const float StructureClearanceYalms = 1.2f;
    private const float StructureCrowdReachYalms = 15f;
    private const float StructureMeleeStandoff = 2f;
    private const float StructureRangedStandoff = 12f;

    private readonly BurstMeter burst = new();
    private readonly CrowdPicker crowdPicker = new();
    private readonly CrowdTracker crowdTracker = new();
    private readonly MountPolicy mountPolicy = new();
    private readonly DriftingSlot formationSlot = new(FormationDriftYalmsPerSec, FormationHoldMinMs, FormationHoldMaxMs);
    private FrontlineProfile profile = FrontlineProfile.For(PvpStrategy.Moderate);
    private Persona persona = Persona.Neutral;
    private Vector3? axis;
    private bool hurt;
    private ulong lastTargetId;

    public bool UnderBurst { get; private set; }
    public bool CrowdRiding { get; private set; }
    public bool TeamOnTheMove => crowdTracker.Moving;
    protected virtual bool FollowTeamObjectivesOnly => false;

    public void SetStrategy(PvpStrategy strategy, CustomFrontlineProfile? custom = null)
        => profile = FrontlineProfile.For(strategy, custom);

    public void SetPersona(in Persona value) => persona = value;

    public void MarkBase(Vector3 position) => crowdPicker.MarkBase(position);

    public void OnDeath() => mountPolicy.Reset();

    public MountDecision DecideMount(in MountInputs inputs, long nowMs)
        => mountPolicy.Decide(in inputs, profile.MountDistance, profile.EnemyAwareRadius, in persona, nowMs);

    public void Reset()
    {
        burst.Reset();
        crowdPicker.Reset();
        crowdTracker.Reset();
        mountPolicy.Reset();
        formationSlot.Reset();
        axis = null;
        hurt = false;
        lastTargetId = 0;
        UnderBurst = false;
        CrowdRiding = false;
    }

    public MovePlan Decide(PvpSnapshot snapshot)
    {
        var plan = ChoosePlan(snapshot);
        return HazardAvoidance.Apply(snapshot, in plan);
    }

    private MovePlan ChoosePlan(PvpSnapshot snapshot)
    {
        UnderBurst = burst.DropPerSecond(snapshot.SelfHp) >= profile.BurstDropPerSec;
        UpdateHurt(snapshot);

        var cluster = crowdPicker.Pick(snapshot, crowdTracker.Position, Environment.TickCount64);
        if (cluster is { } picked)
        {
            crowdTracker.Update(picked.Densest);
        }
        else
        {
            crowdTracker.Stall();
        }
        CrowdRiding = cluster is { IsRiding: true };

        var target = ChooseTarget(snapshot);
        if (crowdTracker.Position is not { } crowd)
        {
            return new MovePlan(MoveKind.Hold, snapshot.Self, snapshot.Self, HoldStopRange, Sprint: false, "waiting for the team",
                Pursue: false, Posture.Hold, target?.Id ?? 0);
        }

        var structure = target is null
            ? TargetScoring.TeamObjective(snapshot, crowd, StructureCrowdReachYalms, profile.EngageRange)
              ?? (FollowTeamObjectivesOnly ? null : TargetScoring.NearestStructure(snapshot, snapshot.Self, profile.EngageRange))
            : null;
        var targetId = target?.Id ?? structure?.Id ?? 0;
        var forward = FormationAxis(snapshot, crowd);
        var right = VectorMath.RightOf(forward);
        var lead = crowdTracker.Lead(CrowdLeadSeconds) * persona.LeadScale;
        var slot = FormationSlot(snapshot.PrefersBackline);
        var distanceToCrowd = Vector3.Distance(snapshot.Self, crowd);

        if (distanceToCrowd > profile.RegroupDistance)
        {
            var rejoin = Settle(snapshot, crowd + lead + right * slot.X);
            return new MovePlan(MoveKind.Engage, rejoin, crowd, TravelStopRange, Sprint: true, $"rejoin the team, {distanceToCrowd:F0}y away",
                Pursue: false, Posture.Regroup, targetId);
        }

        if (hurt)
        {
            var safe = Settle(snapshot, crowd + lead - forward * profile.RetreatOffset + right * slot.X);
            return new MovePlan(MoveKind.Retreat, safe, crowd, HoldStopRange, Sprint: true, $"hurt hp={snapshot.SelfHp:P0}, behind the team",
                Pursue: false, Posture.Retreat, targetId);
        }

        var station = crowd + lead + right * slot.X + forward * slot.Y;
        string reason;
        if (structure is { } objective)
        {
            station = RingAround(objective, station, snapshot.PrefersBackline);
            reason = $"hitting the objective with the team, {objective.AllyVotes} on it";
        }
        else
        {
            reason = target is { } chosen && snapshot.EnemiesWithin(profile.EngageRange) > 0
                ? $"with the team → {(int)(chosen.Hp * 100)}%@{chosen.DistanceToSelf:F0}y"
                : "with the team";
        }
        return new MovePlan(MoveKind.Hold, Settle(snapshot, station), crowd, HoldStopRange, Sprint: false, reason, Pursue: false, Posture.Hold, targetId);
    }

    private Vector2 FormationSlot(bool backline)
    {
        var depth = (backline ? -profile.BackOffset : profile.FrontOffset) + persona.FormationDepthYalms;
        var shape = new SlotShape(new Vector2(persona.FormationLateralYalms, depth),
            new Vector2(FormationLateralWander, FormationDepthWander) * persona.Restlessness, 0f, float.MaxValue);
        return formationSlot.Step(in shape, Environment.TickCount64);
    }

    private Vector3 FormationAxis(PvpSnapshot snapshot, Vector3 crowd)
    {
        var desired = FrontDirection(snapshot, crowd)
                      ?? (crowdTracker.Moving ? VectorMath.FlatDirection(crowdTracker.Velocity) : null)
                      ?? (crowdPicker.Base is { } basePosition ? VectorMath.FlatDirection(crowd - basePosition) : null)
                      ?? VectorMath.FlatDirection(crowd - snapshot.Self)
                      ?? Vector3.UnitZ;
        if (axis is not { } previous)
        {
            axis = desired;
            return desired;
        }

        var toward = Vector3.Dot(previous, desired) < AxisReversalCosine ? VectorMath.RightOf(previous) : desired;
        axis = VectorMath.FlatDirection(Vector3.Lerp(previous, toward, AxisBlend)) ?? toward;
        return axis.Value;
    }

    private static Vector3 Settle(PvpSnapshot snapshot, Vector3 point)
        => Spacing.KeepOutside(snapshot.Structures, Spacing.KeepApart(snapshot.Allies, point, AllySeparationYalms), snapshot.Self,
            StructureClearanceYalms);

    private static Vector3 RingAround(PvpStructure structure, Vector3 preferred, bool backline)
    {
        var standoff = structure.Radius + (backline ? StructureRangedStandoff : StructureMeleeStandoff);
        var bearing = VectorMath.FlatDirection(preferred - structure.Position) ?? Vector3.UnitX;
        return (structure.Position + bearing * standoff) with { Y = preferred.Y };
    }

    private void UpdateHurt(PvpSnapshot snapshot)
    {
        if (!hurt)
        {
            hurt = snapshot.SelfHp <= profile.HurtHp || (snapshot.FocusCount >= profile.FocusHurtCount && snapshot.SelfHp <= profile.FocusedHurtHp);
            return;
        }
        if (snapshot.SelfHp >= profile.RecoveredHp && snapshot.FocusCount < profile.FocusHurtCount)
        {
            hurt = false;
        }
    }

    private static Vector3? FrontDirection(PvpSnapshot snapshot, Vector3 crowd)
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
        return count == 0 ? null : VectorMath.FlatDirection(sum / count - crowd);
    }

    private PvpActor? ChooseTarget(PvpSnapshot snapshot)
    {
        var best = TargetScoring.Best(snapshot, profile.EngageRange, lastTargetId);
        lastTargetId = best?.Id ?? 0;
        return best;
    }
}
