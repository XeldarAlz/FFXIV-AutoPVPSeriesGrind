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
        await DelayMs(StartupSettleMs);

        PvpAutoLbIpc.Instance.PushPresetsIfNeeded();

        SetPhase(AutoPhase.Queueing);
    }

    private async Task<bool> TickOutOfDuty()
    {
        if (completion.Active)
        {
            if (completion.Confirmed) CompleteMatch("confirmed duty completed before exit");
            else
            {
                session.UnconfirmedDepartures++;
                session.StopReason = "Left the duty without completion evidence; this match is not counted.";
                Warn(session.StopReason);
                ResetDutyState("unconfirmed duty departure");
                return true;
            }
        }

        if (matchFlow.InMatchLive || matchFlow.AnnouncedEntered || matchFlow.RanSafetyMoveThisDuty || matchFlow.BaselineCaptured)
        {
            ResetDutyState("out of duty");
        }

        var state = DutyOps.ReadQueueState();
        var decision = queueSubmission.Observe(state, Environment.TickCount64);
        var queued = decision == QueueDecision.InFlight;
        if (state == QueueState.Idle)
        {
            EvaluateStopCondition();
        }

        if (stopAfterCurrentMatch && state == QueueState.Idle)
        {
            LogDiagnostic("stop condition reached -> ending run");
            return true;
        }

        if (stopRequested && state == QueueState.Idle && decision != QueueDecision.Wait)
        {
            LogDiagnostic("stop requested and nothing in flight -> ending run");
            return true;
        }

        SetPhase(AutoPhase.Queueing);

        if (decision == QueueDecision.Failed)
        {
            session.StopReason = state == QueueState.Unknown ? "Queue state remained unavailable; run stopped."
                : "Queue registration failed or timed out; run stopped.";
            Warn(session.StopReason);
            return true;
        }
        if (decision == QueueDecision.Wait)
        {
            Status = state == QueueState.Unknown ? "Waiting for readable queue state" : "Verifying queue registration";
            await DelayMs(MainLoopIdleMs);
            return false;
        }
        if (queued && (!queueSubmission.HasSubmitted || !DutyOps.IsQueuedFor(queueRoute)))
        {
            session.StopReason = "Another queue or duty is in flight; run stopped without changing it.";
            Warn(session.StopReason);
            return true;
        }

        if (queued) Status = $"Queued for {queueLabel}";

        if (!queued)
        {
            if (await TryShopBetweenMatches())
            {
                return false;
            }

            var waitMs = nextQueueAllowedAtMs - Environment.TickCount64;
            if (waitMs > 0)
            {
                Status = onBreak ? $"On a break, {FormatRemaining(waitMs)} left" : $"Next match in {FormatRemaining(waitMs)}";
                await DelayMs(MainLoopIdleMs);
                return false;
            }

            var penaltyMinutes = DutyOps.QueuePenaltyMinutes();
            if (penaltyMinutes > 0)
            {
                if (!queuePenaltyAnnounced)
                {
                    queuePenaltyAnnounced = true;
                    LogDiagnostic($"queue penalty active (~{penaltyMinutes} min) -> waiting it out");
                }

                Status = $"Queue penalty, {penaltyMinutes}m left";
                await DelayMs(MainLoopIdleMs);
                return false;
            }

            queuePenaltyAnnounced = false;
            LogDiagnostic($"submitting {queueLabel}, attempt {queueSubmission.Attempts + 1}/{QueueSubmission.MaxAttempts}");
            var dispatched = DutyOps.QueueMatch(queueRoute);
            queueSubmission.Submitted(Environment.TickCount64, dispatched);
            Status = dispatched ? "Verifying queue registration" : "Queue submission failed; waiting before retry";
        }

        await DelayMs(MainLoopIdleMs);
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
