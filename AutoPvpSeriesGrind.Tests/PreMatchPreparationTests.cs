using AutoPvpSeriesGrind.Core.Tasks;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class PreMatchPreparationTests
{
    [Fact]
    public void MountsThenHoldsThroughEntireCountdownUntilDutyStarts()
    {
        var preparation = new PreMatchPreparation();
        Assert.Equal(PreMatchDecision.Mount, preparation.Observe(false, false, true, 0));
        // The mount cast is still in progress; then the character stays mounted.
        for (var now = 500; now < 3000; now += 500)
            Assert.Equal(PreMatchDecision.Hold, preparation.Observe(false, false, false, now));
        for (var now = 3000; now <= 60_000; now += 500)
            Assert.Equal(PreMatchDecision.Hold, preparation.Observe(false, true, true, now));
        Assert.Equal(PreMatchDecision.Start, preparation.Observe(true, true, true, 60_500));
    }

    [Fact]
    public void UnavailableMountNeverAllowsEarlyMovementOrBlocksMatchStart()
    {
        var preparation = new PreMatchPreparation();
        for (var now = 0; now <= 60_000; now += 500)
            Assert.Equal(PreMatchDecision.Hold, preparation.Observe(false, false, false, now));
        Assert.Equal(PreMatchDecision.Start, preparation.Observe(true, false, false, 60_500));
    }

    [Fact]
    public void FailedMountRequestsAreSpacedAndDoNotDelayGateOpening()
    {
        var preparation = new PreMatchPreparation();
        Assert.Equal(PreMatchDecision.Mount, preparation.Observe(false, false, true, 0));
        Assert.Equal(PreMatchDecision.Hold, preparation.Observe(false, false, true, 500));
        Assert.Equal(PreMatchDecision.Hold, preparation.Observe(false, false, true, 2499));
        Assert.Equal(PreMatchDecision.Mount, preparation.Observe(false, false, true, 2500));
        Assert.Equal(PreMatchDecision.Start, preparation.Observe(true, false, true, 2600));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StartingAutomationInLiveDutySkipsPreMatchPreparation(bool mounted)
    {
        var preparation = new PreMatchPreparation();
        Assert.Equal(PreMatchDecision.Start, preparation.Observe(true, mounted, true, 0));
    }

    [Fact]
    public void NextMatchStartsWithFreshPreparation()
    {
        var preparation = new PreMatchPreparation();
        Assert.Equal(PreMatchDecision.Mount, preparation.Observe(false, false, true, 0));
        Assert.Equal(PreMatchDecision.Start, preparation.Observe(true, true, false, 500));
        preparation.Reset();
        Assert.Equal(PreMatchDecision.Mount, preparation.Observe(false, false, true, 1000));
        Assert.Equal(PreMatchDecision.Hold, preparation.Observe(false, true, true, 1500));
    }
}
