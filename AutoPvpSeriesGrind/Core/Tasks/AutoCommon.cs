using clib.TaskSystem;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal abstract class AutoCommon : TaskBase
{
    protected void LogDiagnostic(string message, [CallerFilePath] string callerFile = "") => RunLog.Info(message, callerFile);

    protected void Warn(string message, [CallerFilePath] string callerFile = "") => RunLog.Warning(message, callerFile);

    protected async Task<bool> WaitUntilTimed(Func<bool> condition, int timeoutMs, string scope, int checkMs = 30)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        var threw = false;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested) return false;
            bool ok;
            try
            {
                ok = condition();
            }
            catch (Exception ex)
            {
                if (!threw)
                {
                    Warn($"WaitUntilTimed '{scope}' condition threw (treating as unsatisfied): {ex.Message}");
                    threw = true;
                }
                ok = false;
            }
            if (ok) return true;
            await DelayMs(checkMs);
        }
        LogDiagnostic($"WAIT TIMEOUT: '{scope}' not satisfied within {timeoutMs / 1000}s");
        return false;
    }
}
