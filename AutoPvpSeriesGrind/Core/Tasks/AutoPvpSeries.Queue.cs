using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries
{
    private const int StartupSettleMs = 1000;
    private const int MillisecondsPerMinute = 60_000;
    private const int BreakJitterSpreadDivisor = 5;

    private async Task Startup()
    {
        await NextFrame(StartupSettleMs);

        PvpAutoLbIpc.Instance.PushPresetsIfNeeded();

        SetPhase(AutoPhase.Queueing);
        DutyOps.QueueCasualMatch();
    }

    private async Task<bool> TickOutOfDuty()
    {
        if (matchFlow.InMatchLive || matchFlow.AnnouncedEntered || matchFlow.RanSafetyMoveThisDuty || matchFlow.BaselineCaptured)
        {
            ResetDutyState("out of duty");
        }

        if (stopAfterCurrentMatch)
        {
            LogDiagnostic("stop condition reached -> ending run");
            return true;
        }

        SetPhase(AutoPhase.Queueing);

        if (!Svc.Condition[ConditionFlag.InDutyQueue] && !DutyOps.IsQueued())
        {
            var waitMs = nextQueueAllowedAtMs - Environment.TickCount64;
            if (waitMs > 0)
            {
                Status = onBreak ? $"On a break, {FormatRemaining(waitMs)} left" : $"Next match in {FormatRemaining(waitMs)}";
                await NextFrame(MainLoopIdleMs);
                return false;
            }

            LogDiagnostic("not queued -> queueing casual match roulette");
            DutyOps.QueueCasualMatch();
        }

        await NextFrame(MainLoopIdleMs);
        return false;
    }

    private void ScheduleNextQueue()
    {
        if (stopAfterCurrentMatch) return;

        matchesSinceBreak++;
        long delayMs;
        if (settings.TakeBreaks && settings.BreakEvery > 0 && matchesSinceBreak >= settings.BreakEvery)
        {
            matchesSinceBreak = 0;
            onBreak = true;
            var baseMs = Math.Max(1, settings.BreakMinutes) * MillisecondsPerMinute;
            delayMs = HumanTiming.Jitter(baseMs, baseMs / BreakJitterSpreadDivisor);
            LogDiagnostic($"break scheduled (~{delayMs / 60000.0:F1} min) after {settings.BreakEvery} matches");
        }
        else
        {
            onBreak = false;
            delayMs = RequeueDelayMs();
            if (delayMs > 0) LogDiagnostic($"requeue delay scheduled (~{delayMs / 1000.0:F0}s)");
        }
        nextQueueAllowedAtMs = Environment.TickCount64 + delayMs;
    }

    private long RequeueDelayMs()
    {
        var minSeconds = Math.Max(0, settings.RequeueMinSec);
        var maxSeconds = Math.Max(minSeconds, settings.RequeueMaxSec);
        if (maxSeconds <= 0) return 0;
        return HumanTiming.SharedRandom.Next(minSeconds, maxSeconds + 1) * 1000L;
    }

    private static string FormatRemaining(long milliseconds)
    {
        var totalSeconds = (int)Math.Ceiling(milliseconds / 1000.0);
        return totalSeconds >= 60 ? $"{totalSeconds / 60}m {totalSeconds % 60:00}s" : $"{totalSeconds}s";
    }

    private static unsafe bool TryCommenceDuty()
    {
        if (!AddonProbe.TryGetReady(ApsgConstants.AddonNames.DutyReady, out var addon))
        {
            return false;
        }

        new AddonMaster.ContentsFinderConfirm((nint)addon).Commence();
        return true;
    }
}
