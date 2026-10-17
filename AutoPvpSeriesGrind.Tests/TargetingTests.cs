using AutoPvpSeriesGrind.Core.Combat;
using System.Numerics;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class TargetingTests
{
    private static PvpActor Actor(ulong id, float x, float hp = 1, bool canAttack = true, bool guard = false)
        => new(id, new Vector3(x, 0, 0), hp, PvpRole.Melee, guard, false, false, 0, Math.Abs(x), canAttack);

    private static PvpSnapshot Snapshot(PvpActor[] enemies, PvpActor[]? allies = null, PvpActor[]? regroup = null) => new()
    {
        Self = Vector3.Zero, SelfRotation = 0, SelfId = 100, SelfHp = 1, SelfRole = PvpRole.Melee,
        Objective = null, CurrentTarget = null, Enemies = enemies, Allies = allies ?? [], RegroupAllies = regroup,
        EnemyCentroid = null, FocusCount = 0, Hazards = [], Structures = [],
    };

    [Fact]
    public void ProtectedKillTargetDoesNotBeatAnAttackableTarget()
    {
        var snapshot = Snapshot([Actor(1, 1, .01f, false), Actor(2, 10, .8f)]);
        Assert.Equal(2UL, TargetScoring.Best(snapshot, 25, 1)?.Id);
        Assert.Equal(2UL, TargetScoring.LowestHp(snapshot, 25)?.Id);
    }

    [Fact]
    public void NoEligibleTargetDoesNotFallBackToProtectedOrOutOfRangeEnemy()
    {
        var snapshot = Snapshot([Actor(1, 1, .01f, false), Actor(2, 40)]);
        Assert.Null(TargetScoring.Best(snapshot, 25, 0));
        Assert.Null(TargetScoring.LowestHp(snapshot, 25));
    }

    [Fact]
    public void GuardIsASoftPreferenceSoGuardBreakingRemainsPossible()
    {
        var guarded = Actor(1, 1, .2f, guard: true);
        Assert.Equal(1UL, TargetScoring.LowestHp(Snapshot([guarded]), 25)?.Id);
        Assert.Equal(2UL, TargetScoring.LowestHp(Snapshot([guarded, Actor(2, 5, .8f)]), 25)?.Id);
    }

    [Fact]
    public void RosterHintsGuideRegroupingWithoutInventingCombatAllies()
    {
        var snapshot = Snapshot([], [], [Actor(1, 80, canAttack: false), Actor(2, 85, canAttack: false), Actor(3, 90, canAttack: false)]);
        var picker = new CrowdPicker();
        picker.MarkBase(Vector3.Zero);
        var group = picker.Pick(snapshot, null);
        Assert.NotNull(group);
        Assert.Equal(3, group.Value.Size);
        Assert.Equal(85f, group.Value.Centroid.X);
        Assert.Empty(snapshot.Allies);
        Assert.Equal(0, snapshot.AlliesWithin(100));
    }
}
