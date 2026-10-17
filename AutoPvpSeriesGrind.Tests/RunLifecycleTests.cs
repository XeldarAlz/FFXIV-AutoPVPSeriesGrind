using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Tasks;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class RunLifecycleTests
{
    [Fact]
    public void UnreadableQueueNeverAllowsSubmissionAndEventuallyStops()
    {
        var queue = new QueueSubmission();
        Assert.Equal(QueueDecision.Wait, queue.Observe(QueueState.Unknown, 0));
        Assert.Equal(QueueDecision.Wait, queue.Observe(QueueState.Unknown, 29_999));
        Assert.Equal(QueueDecision.Failed, queue.Observe(QueueState.Unknown, 30_000));
        Assert.Equal(QueueDecision.Submit, queue.Observe(QueueState.Idle, 30_001));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SubmissionFailuresAreSpacedAndBounded(bool dispatched)
    {
        var queue = new QueueSubmission();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var now = attempt * QueueSubmission.RegistrationTimeoutMs;
            Assert.Equal(QueueDecision.Submit, queue.Observe(QueueState.Idle, now));
            queue.Submitted(now, dispatched);
            Assert.Equal(QueueDecision.Wait, queue.Observe(QueueState.Idle, now + 500));
        }
        Assert.Equal(QueueDecision.Failed, queue.Observe(QueueState.Idle, 30_000));
        Assert.Equal(dispatched, queue.HasSubmitted);
    }

    [Theory]
    [InlineData((int)QueueState.Queued)]
    [InlineData((int)QueueState.Ready)]
    [InlineData((int)QueueState.Accepted)]
    public void LateRegistrationPreventsAnotherSubmission(int state)
    {
        var queue = new QueueSubmission();
        queue.Submitted(0, true);
        Assert.Equal(QueueDecision.InFlight, queue.Observe((QueueState)state, 10_001));
        Assert.Equal(0, queue.Attempts);
        Assert.True(queue.HasSubmitted);
    }

    [Fact]
    public void PendingRegistrationCannotHangForever()
    {
        var queue = new QueueSubmission();
        queue.Submitted(0, true);
        Assert.Equal(QueueDecision.InFlight, queue.Observe(QueueState.Registering, 500));
        Assert.Equal(QueueDecision.Failed, queue.Observe(QueueState.Registering, 10_000));
    }

    [Fact]
    public void DutyEntryClearsOwnershipForNextQueue()
    {
        var queue = new QueueSubmission();
        queue.Submitted(0, true);
        queue.Reset();
        Assert.False(queue.HasSubmitted);
        Assert.Equal(0, queue.Attempts);
        Assert.Equal(QueueDecision.Submit, queue.Observe(QueueState.Idle, 1));
    }

    [Fact]
    public void EarlyDepartureIsNotCompletion()
    {
        var match = new MatchCompletion();
        match.Begin(791);
        Assert.False(match.TryCount());
        match.Reset();
        Assert.False(match.TryCount());
    }

    [Fact]
    public void CompletionEventAndResultsCountExactlyOnce()
    {
        var match = new MatchCompletion();
        match.Begin(791);
        match.Confirm(791);
        Assert.True(match.TryCount());
        match.Confirm(791);
        match.Begin(791);
        Assert.False(match.TryCount());
    }

    [Fact]
    public void CompletionCannotLeakAcrossDuties()
    {
        var match = new MatchCompletion();
        match.Confirm(791);
        Assert.False(match.Confirmed);
        match.Begin(791);
        match.Confirm(1032);
        Assert.False(match.TryCount());
        match.Confirm(791);
        Assert.True(match.TryCount());
        match.Reset();
        match.Begin(1032);
        Assert.False(match.TryCount());
    }

    [Theory]
    [InlineData(MatchType.CrystallineConflict, 1032u, "MKSRecord")]
    [InlineData(MatchType.Frontline, 376u, "FrontlineRecord")]
    [InlineData(MatchType.RivalWings, 791u, "ManeuversRecord")]
    public void EachModeHasExplicitTerritoryAndResults(MatchType mode, uint territory, string addon)
    {
        Assert.True(PvpDuty.Contains(mode, territory));
        Assert.Equal(addon, PvpDuty.ResultsAddon(mode));
        Assert.False(PvpDuty.Contains(mode, 0));
    }

    [Fact]
    public void RivalWingsAndInvalidModesCannotFallBackToCc()
    {
        Assert.False(PvpDuty.Contains(MatchType.RivalWings, 1032));
        Assert.False(PvpDuty.Contains(MatchType.CrystallineConflict, 791));
        Assert.False(PvpDuty.Contains((MatchType)99, 1032));
        Assert.Throws<ArgumentOutOfRangeException>(() => PvpDuty.ResultsAddon((MatchType)99));
    }
}
