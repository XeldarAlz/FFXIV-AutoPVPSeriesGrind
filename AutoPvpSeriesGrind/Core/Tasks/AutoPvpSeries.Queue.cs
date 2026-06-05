using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    private async Task Startup()
    {
        await NextFrame(1000);

        Ipc.PvpAutoLbIpc.Instance.PushPresetsIfNeeded();

        Controller_SetPhaseQueueing();
        DutyOps.QueueCasualMatch();
    }

    private void Controller_SetPhaseQueueing()
        => Plugin.Instance.Controller.Phase = AutoPhase.Queueing;

    private async Task<bool> TickOutOfDuty()
    {
        if (inMatchLive || announcedEntered || ranSafetyMoveThisDuty || baselineCaptured)
            ResetDutyState("out of duty");

        if (stopAfterCurrentMatch)
        {
            Diag("stop condition reached -> ending run");
            return true;
        }

        Plugin.Instance.Controller.Phase = AutoPhase.Queueing;

        if (!Svc.Condition[ConditionFlag.InDutyQueue] && !DutyOps.IsQueued())
        {
            var waitMs = nextQueueAllowedAtMs - Environment.TickCount64;
            if (waitMs > 0)
            {
                Status = onBreak ? $"On a break — {FormatRemaining(waitMs)} left" : $"Next match in {FormatRemaining(waitMs)}";
                await NextFrame(MainLoopIdleMs);
                return false;
            }

            Diag("not queued -> queueing casual match roulette");
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
            var baseMs = Math.Max(1, settings.BreakMinutes) * 60_000;
            delayMs = HumanTiming.Jitter(baseMs, baseMs / 5);
            Diag($"break scheduled (~{delayMs / 60000.0:F1} min) after {settings.BreakEvery} matches");
        }
        else
        {
            onBreak = false;
            delayMs = RequeueDelayMs();
            if (delayMs > 0) Diag($"requeue delay scheduled (~{delayMs / 1000.0:F0}s)");
        }
        nextQueueAllowedAtMs = Environment.TickCount64 + delayMs;
    }

    private long RequeueDelayMs()
    {
        var min = Math.Max(0, settings.RequeueMinSec);
        var max = Math.Max(min, settings.RequeueMaxSec);
        if (max <= 0) return 0;
        return HumanTiming.Rng.Next(min, max + 1) * 1000L;
    }

    private static string FormatRemaining(long ms)
    {
        var s = (int)Math.Ceiling(ms / 1000.0);
        return s >= 60 ? $"{s / 60}m {s % 60:00}s" : $"{s}s";
    }

    private static unsafe bool TryCommenceDuty()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(ApsgConstants.AddonNames.DutyReady, out var a)
            || !GenericHelpers.IsAddonReady(a))
            return false;

        new AddonMaster.ContentsFinderConfirm((nint)a).Commence();
        return true;
    }
}
