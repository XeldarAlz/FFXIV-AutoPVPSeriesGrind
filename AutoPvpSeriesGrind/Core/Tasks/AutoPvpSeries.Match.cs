using AutoPvpSeriesGrind.Core.Game;
using Dalamud.Game.ClientState.Conditions;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    private const int PvpAreaWaitMs = 90_000;

    // When the results addon (MKSRecord) appears the match is over: count it, say "Good Match", leave, reset.
    private async Task<bool> TryHandleMatchEnd()
    {
        if (!ResultsScreenVisible()) return false;

        session.MatchesCompleted++;
        Diag($"completed match {session.MatchesCompleted}");

        // Evaluate the active stop mode (match count / Series rank / time / endless) after each win.
        if (Plugin.Cfg.ActiveMode.IsComplete(session.ToModeContext()))
        {
            stopAfterCurrentMatch = true;
            session.CompletedByGoal = true;
            Diag($"stop condition met ({Plugin.Cfg.ActiveMode.DisplayName}) -> stopping after this match");
        }

        if (sendGoodMatch && !goodMatchSent)
        {
            Cmd("/quickchat \"Good Match\"");
            goodMatchSent = true;
            await NextFrame(500);
        }

        Diag("match ended (results screen visible) -> leaving duty");
        Cmd("/vnav stop");
        DutyOps.LeaveCurrentContent();

        await NextFrame(2000);
        ResetDutyState("left duty (post-match)");
        return true;
    }

    private async Task CaptureBaseline()
    {
        Cmd("/vnav stop");
        Diag("in duty -> waiting for PvP area before baseline capture");
        await WaitUntilTimed(MatchState.InPvpArea, PvpAreaWaitMs, "in-pvp-area", checkMs: PollMs);

        dutyBaselineTime = DutyOps.ContentTimeLeft();
        baselineCaptured = true;
        timerMovedFromBaseline = false;
        sawIntroBand = false;
        announcedEntered = false;
        announcedPortrait = false;
        inMatchLive = false;
        ranSafetyMoveThisDuty = false;
        hasEnabledRotationThisLife = false;
        portraitHelloThreshold = rng.Next(1, 30);
        portraitHelloSent = false;
        Plugin.Instance.Controller.Phase = AutoPhase.InMatch;

        Diag($"duty entry baseline ContentTimeLeft -> {dutyBaselineTime}");
        Diag($"portrait hello threshold set -> {portraitHelloThreshold}s");
    }

    // Pre-match: hold position through the portrait/intro band, send a randomized hello, and watch the
    // content timer for the gate opening (timer jumps back above 100s once the match goes live).
    private async Task RunWaitingPhase()
    {
        while (InDuty() && !inMatchLive && !CancelToken.IsCancellationRequested)
        {
            CheckDeathAndReapplyRotation();

            if (!announcedEntered)
            {
                Diag("entered PvP match; waiting for portraits + gate (ContentTimeLeft)");
                Cmd("/vnav stop");
                announcedEntered = true;
                Plugin.Instance.Controller.Phase = AutoPhase.InMatch;
            }

            var tLeft = DutyOps.ContentTimeLeft();

            if (dutyBaselineTime != 0 && tLeft > 0 && Math.Abs(tLeft - dutyBaselineTime) >= 10)
                timerMovedFromBaseline = true;

            if (tLeft is < 32 and > 1)
            {
                sawIntroBand = true;
                if (!announcedPortrait)
                {
                    Diag("intro/portraits phase detected (timer ~31s)");
                    announcedPortrait = true;
                }

                if (sendHello && !portraitHelloSent && tLeft <= portraitHelloThreshold && tLeft > 1)
                {
                    Cmd("/quickchat Hello");
                    portraitHelloSent = true;
                    Diag($"quickchat Hello sent at tLeft={tLeft} (threshold={portraitHelloThreshold})");
                }

                Cmd("/vnav stop");
                await NextFrame(1000);
            }
            else
            {
                var gateOpen = tLeft > 100 && (sawIntroBand || timerMovedFromBaseline);
                if (gateOpen)
                {
                    Diag($"gate open detected by ContentTimeLeft -> {tLeft}");
                    inMatchLive = true;
                    Cmd("/rotation Settings TargetingTypes add LowHP");
                    await NextFrame(PollMs);
                    Cmd("/rotation auto LowHP");
                    hasEnabledRotationThisLife = true;
                    rotationNeedsReset = false;
                    Diag("rotation enabled (match start)");
                    break;
                }
                await NextFrame(PollMs);
            }
        }
    }
}
