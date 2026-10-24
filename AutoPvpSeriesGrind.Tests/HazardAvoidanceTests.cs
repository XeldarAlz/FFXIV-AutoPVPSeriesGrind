using AutoPvpSeriesGrind.Core.Combat;
using System.Numerics;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class HazardAvoidanceTests
{
    private const ulong TargetId = 7;

    private static readonly Hazard CircleAtOrigin = new(HazardShape.Circle, Vector3.Zero, Vector3.UnitZ, 5f, 0f, "Test Circle");

    private static PvpSnapshot Snapshot(Vector3 self, params Hazard[] hazards) => new()
    {
        Self = self, SelfRotation = 0, SelfId = 100, SelfHp = 1, SelfRole = PvpRole.Melee,
        Objective = null, CurrentTarget = null, Enemies = [], Allies = [],
        EnemyCentroid = null, FocusCount = 0, Hazards = hazards, Structures = [],
    };

    private static MovePlan HoldAt(Vector3 destination)
        => new(MoveKind.Hold, destination, destination, 2f, Sprint: false, "with the team", Pursue: false, Posture.Hold, TargetId);

    [Fact]
    public void StandingInATelegraphDodgesOutOfIt()
    {
        var snapshot = Snapshot(Vector3.Zero, CircleAtOrigin);
        var plan = HazardAvoidance.Apply(snapshot, HoldAt(new Vector3(1f, 0f, 0f)));

        Assert.Equal(MoveKind.Retreat, plan.Kind);
        Assert.Equal(Posture.Reposition, plan.Posture);
        Assert.True(plan.Sprint);
        Assert.Equal(TargetId, plan.TargetId);
        Assert.False(CircleAtOrigin.Contains(plan.Destination, 0f));
    }

    [Fact]
    public void DestinationInsideATelegraphIsMovedClear()
    {
        var snapshot = Snapshot(new Vector3(30f, 0f, 0f), CircleAtOrigin);
        var plan = HazardAvoidance.Apply(snapshot, HoldAt(new Vector3(2f, 0f, 0f)));

        Assert.Equal(MoveKind.Hold, plan.Kind);
        Assert.False(CircleAtOrigin.Contains(plan.Destination, 0f));
        Assert.Equal(plan.Destination, plan.Fallback);
        Assert.Contains("skirting Test Circle", plan.Reason);
    }

    [Fact]
    public void PlanWithoutHazardsIsUnchanged()
    {
        var original = HoldAt(new Vector3(2f, 0f, 0f));
        Assert.Equal(original, HazardAvoidance.Apply(Snapshot(new Vector3(30f, 0f, 0f)), original));
    }
}
