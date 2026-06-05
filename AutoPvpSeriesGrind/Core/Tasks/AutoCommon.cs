using clib.TaskSystem;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

public abstract class AutoCommon : TaskBase
{
    protected void Diag(string message) => Svc.Log.Info($"{ApsgConstants.LogPrefix} {message}");

    protected void Warn(string message) => Svc.Log.Warning($"{ApsgConstants.LogPrefix} {message}");

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
