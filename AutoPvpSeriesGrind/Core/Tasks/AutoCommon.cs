using clib.TaskSystem;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal abstract class AutoCommon : TaskBase
{
    private const int ChildUnwindGraceMs = 5_000;

    protected void LogDiagnostic(string message, [CallerFilePath] string callerFile = "") => RunLog.Info(message, callerFile);

    protected void Warn(string message, [CallerFilePath] string callerFile = "") => RunLog.Warning(message, callerFile);

    protected static bool IsFreeToAct()
        => Svc.Objects.LocalPlayer is not null
        && !Svc.Condition[ConditionFlag.InCombat]
        && !Svc.Condition[ConditionFlag.BoundByDuty]
        && !Svc.Condition[ConditionFlag.BetweenAreas]
        && !Svc.Condition[ConditionFlag.Casting];

    protected async Task<bool> RunChild(clib.Services.AutoTask child, int timeoutMs, string scope)
    {
        var completion = new TaskCompletionSource();
        child.Run(() => completion.TrySetResult());
        using var registration = CancelToken.Register(() => CancelChild(child, scope));

        var deadline = Environment.TickCount64 + timeoutMs;
        while (!completion.Task.IsCompleted && Environment.TickCount64 < deadline)
        {
            Status = child.Status;
            await NextFrame();
        }

        if (completion.Task.IsCompleted)
        {
            return true;
        }

        Warn($"WATCHDOG: '{scope}' exceeded {timeoutMs / 1000}s; cancelling it");
        CancelChild(child, scope);
        var graceDeadline = Environment.TickCount64 + ChildUnwindGraceMs;
        while (!completion.Task.IsCompleted && Environment.TickCount64 < graceDeadline)
        {
            await NextFrame();
        }

        return false;
    }

    private void CancelChild(clib.Services.AutoTask child, string scope)
    {
        try
        {
            child.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception exception)
        {
            Warn($"Cancelling '{scope}' threw: {exception.Message}");
        }
    }

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
