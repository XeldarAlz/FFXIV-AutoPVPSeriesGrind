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
    public void SubmissionFailuresBackOffBetweenRoundsAndAreBounded(bool dispatched)
    {
        var queue = new QueueSubmission();
        long now = 0;
        for (var cycle = 0; cycle < QueueSubmission.MaxCycles; cycle++)
        {
            for (var attempt = 0; attempt < QueueSubmission.MaxAttempts; attempt++)
            {
                Assert.Equal(QueueDecision.Submit, queue.Observe(QueueState.Idle, now));
                queue.Submitted(now, dispatched);
                Assert.Equal(QueueDecision.Wait, queue.Observe(QueueState.Idle, now + 500));
                now += QueueSubmission.RegistrationTimeoutMs;
            }
            if (cycle == QueueSubmission.MaxCycles - 1)
            {
                break;
            }

            Assert.Equal(QueueDecision.Backoff, queue.Observe(QueueState.Idle, now));
            Assert.Equal(QueueSubmission.CycleBackoffMs, queue.BackoffRemainingMs(now));
            Assert.Equal(QueueDecision.Backoff, queue.Observe(QueueState.Idle, now + QueueSubmission.CycleBackoffMs - 1));
            now += QueueSubmission.CycleBackoffMs;
        }
        Assert.Equal(QueueDecision.Failed, queue.Observe(QueueState.Idle, now));
        Assert.Equal(dispatched, queue.HasSubmitted);
    }

    [Fact]
    public void RegistrationClearsEarlierFailedRounds()
    {
        var queue = new QueueSubmission();
        long now = 0;
        for (var attempt = 0; attempt < QueueSubmission.MaxAttempts; attempt++)
        {
            queue.Observe(QueueState.Idle, now);
            queue.Submitted(now, true);
            now += QueueSubmission.RegistrationTimeoutMs;
        }
        Assert.Equal(QueueDecision.Backoff, queue.Observe(QueueState.Idle, now));

        Assert.Equal(QueueDecision.InFlight, queue.Observe(QueueState.Queued, now + 1));
        Assert.Equal(0, queue.BackoffRemainingMs(now + 1));
        Assert.Equal(QueueDecision.Submit, queue.Observe(QueueState.Idle, now + 2));
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
        Assert.True(PvpDuty.Contains(mode, territory, UnknownIntendedUse));
        Assert.Equal(addon, PvpDuty.ResultsAddon(mode));
        Assert.False(PvpDuty.Contains(mode, 0, UnknownIntendedUse));
    }

    [Fact]
    public void RivalWingsAndInvalidModesCannotFallBackToCc()
    {
        Assert.False(PvpDuty.Contains(MatchType.RivalWings, 1032, UnknownIntendedUse));
        Assert.False(PvpDuty.Contains(MatchType.CrystallineConflict, 791, UnknownIntendedUse));
        Assert.False(PvpDuty.Contains((MatchType)99, 1032, UnknownIntendedUse));
        Assert.Throws<ArgumentOutOfRangeException>(() => PvpDuty.ResultsAddon((MatchType)99));
    }

    [Theory]
    [InlineData(MatchType.CrystallineConflict, 28u)]
    [InlineData(MatchType.CrystallineConflict, 37u)]
    [InlineData(MatchType.Frontline, 18u)]
    [InlineData(MatchType.RivalWings, 39u)]
    public void UnlistedArenaIsRecognizedByIntendedUse(MatchType mode, uint intendedUse)
    {
        Assert.True(PvpDuty.Contains(mode, UnlistedTerritory, intendedUse));
        Assert.Equal(mode, PvpDuty.ModeOf(UnlistedTerritory, intendedUse));
    }

    [Fact]
    public void NonPvpIntendedUseIsNotAnArena()
    {
        Assert.Null(PvpDuty.ModeOf(UnlistedTerritory, UnknownIntendedUse));
        Assert.Null(PvpDuty.ModeOf(UnlistedTerritory, 41));
    }

    [Fact]
    public void ListedTerritoryWinsOverIntendedUse()
    {
        Assert.Equal(MatchType.RivalWings, PvpDuty.ModeOf(PvpDuty.HiddenGorgeTerritoryId, 18));
    }

    private const uint UnknownIntendedUse = 0;
    private const uint UnlistedTerritory = 65000;
}
