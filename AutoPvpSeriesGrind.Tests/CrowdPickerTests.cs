using AutoPvpSeriesGrind.Core.Combat;
using System.Numerics;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class CrowdPickerTests
{
    private static PvpActor Ally(ulong id, float x)
        => new(id, new Vector3(x, 0, 0), 1, PvpRole.Melee, false, false, false, 0, Math.Abs(x), CanAttack: false);

    private static PvpActor[] Group(ulong firstId, float x, int size)
    {
        var group = new PvpActor[size];
        for (var memberIndex = 0; memberIndex < size; memberIndex++)
        {
            group[memberIndex] = Ally(firstId + (ulong)memberIndex, x + memberIndex * 0.5f);
        }
        return group;
    }

    private static PvpSnapshot Snapshot(PvpActor[] allies) => new()
    {
        Self = Vector3.Zero, SelfRotation = 0, SelfId = 100, SelfHp = 1, SelfRole = PvpRole.Melee,
        Objective = null, CurrentTarget = null, Enemies = [], Allies = allies,
        EnemyCentroid = null, FocusCount = 0, Hazards = [], Structures = [],
    };

    [Fact]
    public void ChainedColumnAnchorsOnItsDenseEnd()
    {
        PvpActor[] column = [Ally(1, 0), Ally(2, 12), Ally(3, 24), Ally(4, 36), .. Group(10, 48, 8)];
        var cluster = Assert.Single(PvpSnapshot.Clusters(column));

        Assert.Equal(12, cluster.Size);
        Assert.True(cluster.Centroid.X < 40f);
        Assert.InRange(cluster.Densest.X, 44f, 52f);
    }

    [Fact]
    public void FollowedGroupIsKeptForASmallGain()
    {
        var kept = Group(1, 20, 7);
        var slightlyBetter = Group(20, 60, 9);
        var snapshot = Snapshot([.. kept, .. slightlyBetter]);
        var followed = PvpSnapshot.Clusters(kept)[0].Densest;
        var picker = new CrowdPicker();

        Assert.Equal(7, picker.Pick(snapshot, followed, 10_000)?.Size);
        Assert.Equal(7, picker.Pick(snapshot, followed, 15_000)?.Size);
    }

    [Fact]
    public void ClearlyBetterGroupWinsAfterTheConfirmDelay()
    {
        var kept = Group(1, 20, 3);
        var muchBigger = Group(20, 100, 12);
        var snapshot = Snapshot([.. kept, .. muchBigger]);
        var followed = PvpSnapshot.Clusters(kept)[0].Densest;
        var picker = new CrowdPicker();

        Assert.Equal(3, picker.Pick(snapshot, followed, 10_000)?.Size);
        Assert.Equal(12, picker.Pick(snapshot, followed, 12_500)?.Size);
    }

    [Fact]
    public void FirstPickWeighsTravelAgainstSize()
    {
        var picker = new CrowdPicker();
        var picked = picker.Pick(Snapshot([.. Group(1, 20, 7), .. Group(20, 300, 8)]), null, 10_000);

        Assert.Equal(7, picked?.Size);
    }

    [Fact]
    public void MuchLargerGroupIsStillPreferredFromScratch()
    {
        var picker = new CrowdPicker();
        var picked = picker.Pick(Snapshot([.. Group(1, 20, 3), .. Group(20, 150, 12)]), null, 10_000);

        Assert.Equal(12, picked?.Size);
    }
}
