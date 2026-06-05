using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Conditions;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;
using static AutoPvpSeriesGrind.Core.ApsgConstants.CrystallineConflict;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    private const int PvpAreaWaitMs = 90_000;

    private const int PostQuickChatMs = 500;
    private const int LeaveDutyTimeoutMs = 10_000;
    private const int PortraitPhasePollMs = 250;

    private async Task<bool> TryHandleMatchEnd()
    {
        if (!ResultsScreenVisible()) return false;

        session.MatchesCompleted++;
        Diag($"completed match {session.MatchesCompleted}");

        if (Plugin.Cfg.ActiveMode.IsComplete(session.ToModeContext()))
        {
            stopAfterCurrentMatch = true;
            session.CompletedByGoal = true;
            Diag($"stop condition met ({Plugin.Cfg.ActiveMode.DisplayName}) -> stopping after this match");
        }

        ScheduleNextQueue();

        await LingerThenLeave();
        ResetDutyState("left duty (post-match)");
        return true;
    }

    // The leave delay is how long we sit on the results screen before bailing. "Good Match" is sent only if
    // its random delay lands inside that window — a short leave delay means we just leave early without it.
    private async Task LingerThenLeave()
    {
        var lingerMs = Math.Max(0, settings.LeaveDutyDelayMs);
        var waitedMs = 0;

        if (greeting.PlanGoodMatchDelayMs(settings) is { } goodbyeAtMs)
        {
            if (goodbyeAtMs <= lingerMs)
            {
                if (goodbyeAtMs > 0) await NextFrame(goodbyeAtMs);
                Cmd(GameCommands.QuickChatGoodMatch);
                await NextFrame(PostQuickChatMs);
                waitedMs = goodbyeAtMs + PostQuickChatMs;
            }
            else
            {
                Diag($"leaving early -> skipped \"Good Match\" (delay {goodbyeAtMs}ms > leave delay {lingerMs}ms)");
            }
        }

        if (waitedMs < lingerMs)
            await NextFrame(lingerMs - waitedMs);

        Diag("match ended (results screen visible) -> leaving duty");
        Cmd(GameCommands.NavStop);
        DutyOps.LeaveCurrentContent();
        await WaitUntilTimed(() => !InDuty(), LeaveDutyTimeoutMs, "left-duty", checkMs: 100);
    }

    private async Task CaptureBaseline()
    {
        Cmd(GameCommands.NavStop);
        Diag("in duty -> waiting for PvP area before baseline capture");
        await WaitUntilTimed(MatchState.InPvpArea, PvpAreaWaitMs, "in-pvp-area", checkMs: PollMs);

        ResetMatchFlow();
        dutyBaselineTime = DutyOps.ContentTimeLeft();
        baselineCaptured = true;
        greeting.PrepareForMatch(settings);
        Plugin.Instance.Controller.Phase = AutoPhase.InMatch;

        Diag($"duty entry baseline ContentTimeLeft -> {dutyBaselineTime}");
    }

    private async Task RunWaitingPhase()
    {
        while (InDuty() && !inMatchLive && !CancelToken.IsCancellationRequested)
        {
            rotation.TickDeathAndRespawn();

            if (!announcedEntered)
            {
                Diag("entered PvP match; waiting for portraits + gate (ContentTimeLeft)");
                Cmd(GameCommands.NavStop);
                announcedEntered = true;
                Plugin.Instance.Controller.Phase = AutoPhase.InMatch;
            }

            var tLeft = DutyOps.ContentTimeLeft();

            if (dutyBaselineTime != 0 && tLeft > 0 && Math.Abs(tLeft - dutyBaselineTime) >= BaselineMovedThresholdSec)
                timerMovedFromBaseline = true;

            if (tLeft is < IntroBandUpperSec and > IntroBandLowerSec)
            {
                sawIntroBand = true;
                if (!announcedPortrait)
                {
                    Diag("intro/portraits phase detected (timer ~31s)");
                    announcedPortrait = true;
                }

                greeting.TryPortraitGreeting(tLeft, settings);

                movement.HaltPathing();
                await NextFrame(PortraitPhasePollMs);
            }
            else
            {
                var gateOpen = tLeft > GateOpenSec && (sawIntroBand || timerMovedFromBaseline);
                if (gateOpen)
                {
                    Diag($"gate open detected by ContentTimeLeft -> {tLeft}");
                    inMatchLive = true;
                    Cmd(GameCommands.AddLowHpTargeting);
                    await NextFrame(PollMs);
                    Cmd(GameCommands.EnableRotation);
                    rotation.MarkRotationEnabled();
                    Diag("rotation enabled (match start)");
                    break;
                }
                await NextFrame(PollMs);
            }
        }
    }
}
