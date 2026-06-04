using clib.TaskSystem;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

// Thin base over clib's TaskBase: a logging shorthand and a cancellable, time-bounded wait. TaskBase
// itself supplies NextFrame, Status, CancelToken, ErrorIf and the Execute() entry point.
public abstract class AutoCommon : TaskBase
{
    protected void Diag(string message) => Svc.Log.Info($"{ApsgConstants.LogPrefix} {message}");

    protected void Warn(string message) => Svc.Log.Warning($"{ApsgConstants.LogPrefix} {message}");

    // Polls condition until it returns true or the deadline passes. A throwing condition is treated as
    // unsatisfied (logged once) so a transient game-state read can't abort the wait. Returns false on
    // timeout or run cancellation.
    protected async Task<bool> WaitUntilTimed(Func<bool> condition, int timeoutMs, string scope, int checkMs = 30)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        var threw = false;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested) return false;
            bool ok;
            try { ok = condition(); }
            catch (Exception ex)
            {
                if (!threw) { Warn($"WaitUntilTimed '{scope}' condition threw (treating as unsatisfied): {ex.Message}"); threw = true; }
                ok = false;
            }
            if (ok) return true;
            await NextFrame(checkMs);
        }
        Diag($"WAIT TIMEOUT: '{scope}' not satisfied within {timeoutMs / 1000}s");
        return false;
    }
}
