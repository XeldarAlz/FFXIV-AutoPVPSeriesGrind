using System.Threading;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

// A detour may start two tasks. Both belong to the same cancellation lifetime,
// including when one submission fails or an older provider ignores cancellation.
internal sealed class NavigationRequest : IDisposable
{
    private readonly CancellationTokenSource cancellation = new();
    private readonly List<Task> tasks = [];
    private bool disposed;
    public CancellationToken Token { get; }

    public NavigationRequest() => Token = cancellation.Token;

    public Task<T>? Track<T>(Task<T>? task)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (task is not null) tasks.Add(task);
        return task;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { cancellation.Cancel(); }
        catch (AggregateException) { /* Provider cancellation callbacks must not prevent cleanup. */ }
        finally
        {
            cancellation.Dispose();
            foreach (var task in tasks)
                _ = task.ContinueWith(static completed => { _ = completed.Exception; }, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            tasks.Clear();
        }
    }
}
