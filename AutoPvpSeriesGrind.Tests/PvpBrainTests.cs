using System.Numerics;
using AutoPvpSeriesGrind.Core.Combat;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class PvpBrainTests
{
    private const ulong SelfId = 999;
    private static readonly Vector3 SafeAnchor = new(0f, 0f, -50f);

    private static PvpBrain NewBrain(PvpStrategy strategy = PvpStrategy.Moderate)
    {
        var brain = new PvpBrain(strategy);
        brain.OwnsTargeting = true;
        return brain;
    }

    private static PvpActor Enemy(float hp, float distance, PvpRole role = PvpRole.Melee, ulong id = 1,
        ulong targetId = 0, bool guard = false, Vector3? position = null)
        => new(id, position ?? new Vector3(distance, 0f, 0f), hp, role, guard, false, targetId, distance);

    private static PvpActor Ally(float hp, float distance, ulong id = 100)
        => new(id, new Vector3(distance, 0f, 0f), hp, PvpRole.Melee, false, false, 0, distance);

    private static PvpSnapshot Snap(
        float selfHp,
        PvpRole selfRole = PvpRole.Melee,
        IReadOnlyList<PvpActor>? enemies = null,
        IReadOnlyList<PvpActor>? allies = null,
        Vector3? objective = null,
        int focusCount = 0)
    {
        enemies ??= [];
        allies ??= [];

        Vector3? enemyCentroid = null;
        if (enemies.Count > 0)
        {
            var sum = Vector3.Zero;
            for (var enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                sum += enemies[enemyIndex].Position;
            }
            enemyCentroid = sum / enemies.Count;
        }

        return new PvpSnapshot
        {
            Self = Vector3.Zero,
            SelfRotation = 0f,
            SelfId = SelfId,
            SelfHp = selfHp,
            SelfRole = selfRole,
            Objective = objective,
            CurrentTarget = null,
            Enemies = enemies,
            Allies = allies,
            EnemyCentroid = enemyCentroid,
            FocusCount = focusCount,
        };
    }

    [Fact]
    public void PanicHp_Retreats()
    {
        var brain = NewBrain();
        var snapshot = Snap(0.1f, enemies: [Enemy(1f, 8f)]);

        var plan = brain.Decide(snapshot, SafeAnchor);

        Assert.Equal(MoveKind.Retreat, plan.Kind);
        Assert.Equal(Posture.Retreat, plan.Posture);
    }

    [Fact]
    public void HealthyWithNoThreat_HoldsInsteadOfRetreating()
    {
        var brain = NewBrain();
        var snapshot = Snap(1f);

        var plan = brain.Decide(snapshot, SafeAnchor);

        Assert.NotEqual(MoveKind.Retreat, plan.Kind);
        Assert.Equal(Posture.Hold, plan.Posture);
    }

    [Fact]
    public void IsolatedAgainstTwoEnemies_Regroups()
    {
        var brain = NewBrain();
        var snapshot = Snap(1f, enemies: [Enemy(1f, 10f, id: 1), Enemy(1f, 10f, id: 2, position: new Vector3(0f, 0f, 10f))]);

        var plan = brain.Decide(snapshot, SafeAnchor);

        Assert.Equal(MoveKind.Retreat, plan.Kind);
        Assert.Equal(Posture.Regroup, plan.Posture);
    }

    [Fact]
    public void Engage_ChoosesLowestHpTarget()
    {
        var brain = NewBrain();
        var enemies = new[]
        {
            Enemy(0.3f, 10f, id: 1),
            Enemy(0.9f, 10f, id: 2, position: new Vector3(0f, 0f, 10f)),
        };
        var snapshot = Snap(1f, enemies: enemies, allies: [Ally(1f, 8f)], objective: Vector3.Zero);

        var plan = brain.Decide(snapshot, SafeAnchor);

        Assert.Equal(1UL, plan.TargetId);
    }

    [Fact]
    public void Engage_PrefersHealerAtEqualValue()
    {
        var brain = NewBrain();
        var enemies = new[]
        {
            Enemy(0.8f, 10f, PvpRole.Melee, id: 1),
            Enemy(0.8f, 10f, PvpRole.Healer, id: 2, position: new Vector3(0f, 0f, 10f)),
        };
        var snapshot = Snap(1f, enemies: enemies, allies: [Ally(1f, 8f)], objective: Vector3.Zero);

        var plan = brain.Decide(snapshot, SafeAnchor);

        Assert.Equal(2UL, plan.TargetId);
    }

    [Fact]
    public void Engage_AvoidsGuardedTargetEvenWhenLowHp()
    {
        var brain = NewBrain();
        var enemies = new[]
        {
            Enemy(0.2f, 10f, PvpRole.Melee, id: 1, guard: true),
            Enemy(0.8f, 10f, PvpRole.Melee, id: 2, position: new Vector3(0f, 0f, 10f)),
        };
        var snapshot = Snap(1f, enemies: enemies, allies: [Ally(1f, 8f)], objective: Vector3.Zero);

        var plan = brain.Decide(snapshot, SafeAnchor);

        Assert.Equal(2UL, plan.TargetId);
    }

    [Fact]
    public void Escalation_ToRetreatAppliesImmediately()
    {
        var brain = NewBrain();
        brain.Decide(Snap(1f, enemies: [Enemy(1f, 10f)], allies: [Ally(1f, 8f)], objective: Vector3.Zero), SafeAnchor);

        var plan = brain.Decide(Snap(0.1f, enemies: [Enemy(1f, 8f)]), SafeAnchor);

        Assert.Equal(MoveKind.Retreat, plan.Kind);
        Assert.Equal(Posture.Retreat, plan.Posture);
    }

    [Fact]
    public void BelowReengageHp_StaysRetreating()
    {
        var brain = NewBrain();
        brain.Decide(Snap(0.1f, enemies: [Enemy(1f, 8f)]), SafeAnchor);

        var plan = brain.Decide(Snap(0.5f), SafeAnchor);

        Assert.Equal(MoveKind.Retreat, plan.Kind);
    }

    [Fact]
    public void AboveReengageHp_AfterDwell_Reengages()
    {
        var brain = NewBrain();
        brain.Decide(Snap(0.1f, enemies: [Enemy(1f, 8f)]), SafeAnchor);
        brain.Decide(Snap(0.5f), SafeAnchor);

        Thread.Sleep(800);
        var plan = brain.Decide(Snap(0.9f), SafeAnchor);

        Assert.NotEqual(MoveKind.Retreat, plan.Kind);
    }
}
