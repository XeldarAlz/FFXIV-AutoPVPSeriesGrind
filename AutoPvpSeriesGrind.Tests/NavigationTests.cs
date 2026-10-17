using AutoPvpSeriesGrind.Core.Tasks;
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
}
