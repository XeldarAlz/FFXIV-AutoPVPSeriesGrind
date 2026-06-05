using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Conditions;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    private const int PvpAreaWaitMs = 90_000;

    private const double EmoteChance = 0.35;

    private const int PostQuickChatMs = 500;
    private const int LeaveDutyTimeoutMs = 10_000;
    private const int PortraitPhasePollMs = 1000;

    // ContentTimeLeft (seconds) bands used to read the pre-match flow. During the portrait/intro phase
    // the timer counts down inside the intro band; once the gate opens it jumps back up past GateOpenSec.
    private const int IntroBandUpperSec = 32;
    private const int IntroBandLowerSec = 1;
    private const int GateOpenSec = 100;
    private const int BaselineMovedThresholdSec = 10;

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
        var lingerMs = Math.Max(0, leaveDutyDelayMs);
        var waitedMs = 0;

        if (sendGoodMatch && !goodMatchSent)
        {
            goodMatchSent = true;
            if (HumanTiming.Maybe(goodMatchChance))
            {
                var goodbyeAtMs = RandSecInclusive(goodMatchDelayMinSec, goodMatchDelayMaxSec) * 1000;
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
        }

        if (waitedMs < lingerMs)
            await NextFrame(lingerMs - waitedMs);

        Diag("match ended (results screen visible) -> leaving duty");
        Cmd(GameCommands.NavStop);
        DutyOps.LeaveCurrentContent();
        await WaitUntilTimed(() => !InDuty(), LeaveDutyTimeoutMs, "left-duty", checkMs: 100);
    }

    private static int RandSecInclusive(int minSec, int maxSec)
    {
        var min = Math.Max(0, minSec);
        var max = Math.Max(min, maxSec);
        return min == max ? min : rng.Next(min, max + 1);
    }

    private async Task CaptureBaseline()
    {
        Cmd(GameCommands.NavStop);
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
        var helloDelay = RandSecInclusive(helloDelayMinSec, helloDelayMaxSec);
        portraitHelloThreshold = Math.Clamp(IntroBandUpperSec - helloDelay, IntroBandLowerSec + 1, IntroBandUpperSec - 1);
        portraitHelloSent = false;
        Plugin.Instance.Controller.Phase = AutoPhase.InMatch;

        Diag($"duty entry baseline ContentTimeLeft -> {dutyBaselineTime}");
        Diag($"portrait hello threshold set -> {portraitHelloThreshold}s");
    }

    private async Task RunWaitingPhase()
    {
        while (InDuty() && !inMatchLive && !CancelToken.IsCancellationRequested)
        {
            CheckDeathAndReapplyRotation();

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

                var greetMoment = !portraitHelloSent && tLeft <= portraitHelloThreshold && tLeft > IntroBandLowerSec;
                if (greetMoment && (sendHello || randomEmotes))
                {
                    portraitHelloSent = true;
                    if (sendHello && HumanTiming.Maybe(helloChance))
                    {
                        Cmd(GameCommands.QuickChatHello);
                        Diag($"quickchat Hello sent at tLeft={tLeft} (threshold={portraitHelloThreshold})");
                    }
                    if (randomEmotes && HumanTiming.Maybe(EmoteChance))
                    {
                        var emote = GameCommands.GreetEmotes[rng.Next(GameCommands.GreetEmotes.Length)];
                        Cmd(emote);
                        Diag($"random emote '{emote}' sent at tLeft={tLeft}");
                    }
                }

                Cmd(GameCommands.NavStop);
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
