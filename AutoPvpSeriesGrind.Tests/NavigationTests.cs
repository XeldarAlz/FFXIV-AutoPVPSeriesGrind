using AutoPvpSeriesGrind.Core.Tasks;
using System.Numerics;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class NavigationTests
{
    [Fact]
    public async Task StoppingADetourCancelsBothLegs()
    {
        var request = new NavigationRequest();
        var first = new TaskCompletionSource<int>();
        var second = new TaskCompletionSource<int>();
        using var firstRegistration = request.Token.Register(() => first.TrySetCanceled(request.Token));
        using var secondRegistration = request.Token.Register(() => second.TrySetCanceled(request.Token));
        _ = request.Track(first.Task);
        _ = request.Track(second.Task);
        request.Dispose();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.Task);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second.Task);
    }

    [Fact]
    public void FailedSecondSubmissionStillCancelsTheFirst()
    {
        var request = new NavigationRequest();
        var first = new TaskCompletionSource<int>();
        using var registration = request.Token.Register(() => first.TrySetCanceled());
        request.Track(first.Task);
        Assert.Null(request.Track<int>(null));
        request.Dispose();
        Assert.True(first.Task.IsCanceled);
    }

    [Fact]
    public void FaultingCancellationCallbackDoesNotBreakCleanup()
    {
        var request = new NavigationRequest();
        using var registration = request.Token.Register(() => throw new InvalidOperationException("provider failure"));
        request.Track(Task.FromException<int>(new InvalidOperationException("path failure")));
        request.Dispose();
        request.Dispose();
        Assert.True(request.Token.IsCancellationRequested);
        Assert.Throws<ObjectDisposedException>(() => { _ = request.Track(Task.FromResult(1)); });
    }

    [Fact]
    public void OldRequestCancellationDoesNotCancelReplacement()
    {
        var old = new NavigationRequest();
        using var replacement = new NavigationRequest();
        old.Dispose();
        Assert.True(old.Token.IsCancellationRequested);
        Assert.False(replacement.Token.IsCancellationRequested);
    }

    [Fact]
    public void RepeatedStallsAtOneSpotEscalateWhateverTheDestination()
    {
        var snags = new SnagTracker();
        var spot = new Vector3(80f, 0f, 75f);
        Assert.Equal(1, snags.Strike(spot, 1_000));
        Assert.Equal(2, snags.Strike(spot + new Vector3(1f, 0f, 0.5f), 4_000));
        Assert.Equal(3, snags.Strike(spot, 16_000));
    }

    [Fact]
    public void AStallSomewhereElseStartsOver()
    {
        var snags = new SnagTracker();
        var spot = new Vector3(80f, 0f, 75f);
        snags.Strike(spot, 1_000);
        snags.Strike(spot, 4_000);
        Assert.Equal(1, snags.Strike(spot + new Vector3(10f, 0f, 0f), 7_000));
    }

    [Fact]
    public void AnOldStallDoesNotCount()
    {
        var snags = new SnagTracker();
        var spot = new Vector3(80f, 0f, 75f);
        snags.Strike(spot, 1_000);
        Assert.Equal(1, snags.Strike(spot, 40_000));
    }

    [Fact]
    public void BackOffStepsAwayFromTheSnagAndAlternatesSides()
    {
        var self = new Vector3(80f, 0f, 75f);
        var facing = Vector3.UnitZ;
        var left = SnagTracker.BackOffPoint(self, facing, 2, -1);
        var right = SnagTracker.BackOffPoint(self, facing, 2, 1);
        var farther = SnagTracker.BackOffPoint(self, facing, 3, 1);
        var capped = SnagTracker.BackOffPoint(self, facing, 9, 1);
        Assert.True(left.Z < self.Z && right.Z < self.Z);
        Assert.True(MathF.Sign(left.X - self.X) == -MathF.Sign(right.X - self.X));
        Assert.True(Vector3.Distance(self, farther) > Vector3.Distance(self, right));
        Assert.Equal(SnagTracker.BackOffPoint(self, facing, 4, 1), capped);
    }
}
