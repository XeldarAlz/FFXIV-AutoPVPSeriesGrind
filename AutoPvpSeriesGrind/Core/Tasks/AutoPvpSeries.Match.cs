using AutoPvpSeriesGrind.Core.Game;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;
using static AutoPvpSeriesGrind.Core.ApsgConstants.CrystallineConflict;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries
{
    private const int PvpAreaWaitMs = 90_000;

    private const int PostQuickChatMs = 500;
    private const int LeaveDutyTimeoutMs = 10_000;
    private const int PortraitPhasePollMs = 250;

    private async Task<bool> TryHandleMatchEnd()
    {
        if (!ResultsScreenVisible()) return false;

        session.MatchesCompleted++;
        LogDiagnostic($"completed match {session.MatchesCompleted}");

        if (Plugin.Cfg.ActiveMode.IsComplete(session.ToModeContext()))
        {
            stopAfterCurrentMatch = true;
            session.CompletedByGoal = true;
            LogDiagnostic($"stop condition met ({Plugin.Cfg.ActiveMode.DisplayName}) -> stopping after this match");
        }

        ScheduleNextQueue();

        await LingerThenLeave();
        ResetDutyState("left duty (post-match)");
        return true;
    }

    // The leave delay is how long we sit on the results screen before bailing. "Good Match" is sent only if
    // its random delay lands inside that window; a short leave delay means we just leave early without it.
    private async Task LingerThenLeave()
    {
        var lingerMs = Math.Max(0, settings.LeaveDutyDelayMs);
        var waitedMs = 0;

        if (greeting.PlanGoodMatchDelayMs(settings) is { } goodbyeAtMs)
        {
            if (goodbyeAtMs <= lingerMs)
            {
                if (goodbyeAtMs > 0) await NextFrame(goodbyeAtMs);
                ExecuteGameCommand(GameCommands.QuickChatGoodMatch);
                await NextFrame(PostQuickChatMs);
                waitedMs = goodbyeAtMs + PostQuickChatMs;
            }
            else
            {
                LogDiagnostic($"leaving early -> skipped \"Good Match\" (delay {goodbyeAtMs}ms > leave delay {lingerMs}ms)");
            }
        }

        if (waitedMs < lingerMs)
            await NextFrame(lingerMs - waitedMs);

        LogDiagnostic("match ended (results screen visible) -> leaving duty");
        ExecuteGameCommand(GameCommands.NavStop);
        DutyOps.LeaveCurrentContent();
        await WaitUntilTimed(() => !InDuty(), LeaveDutyTimeoutMs, "left-duty", checkMs: PollMs);
    }

    private async Task CaptureBaseline()
    {
        ExecuteGameCommand(GameCommands.NavStop);
        LogDiagnostic("in duty -> waiting for PvP area before baseline capture");
        await WaitUntilTimed(MatchState.InPvpArea, PvpAreaWaitMs, "in-pvp-area", checkMs: PollMs);

        ResetMatchFlow();
        matchFlow.DutyBaselineTime = DutyOps.ContentTimeLeft();
        matchFlow.BaselineCaptured = true;
        greeting.PrepareForMatch(settings);
        SetPhase(AutoPhase.InMatch);

        LogDiagnostic($"duty entry baseline ContentTimeLeft -> {matchFlow.DutyBaselineTime}");
    }

    private async Task RunWaitingPhase()
    {
        while (InDuty() && !matchFlow.InMatchLive && !CancelToken.IsCancellationRequested)
        {
            rotation.TickDeathAndRespawn();

            AnnounceMatchEntryOnce();

            var timeLeftSeconds = DutyOps.ContentTimeLeft();

            if (matchFlow.DutyBaselineTime != 0 && timeLeftSeconds > 0 && Math.Abs(timeLeftSeconds - matchFlow.DutyBaselineTime) >= BaselineMovedThresholdSec)
            {
                matchFlow.TimerMovedFromBaseline = true;
            }

            if (timeLeftSeconds is < IntroBandUpperSec and > IntroBandLowerSec)
            {
                await TickIntroBand(timeLeftSeconds);
            }
            else
            {
                if (await TryStartLiveMatch(timeLeftSeconds))
                {
                    break;
                }

                await NextFrame(PollMs);
            }
        }
    }

    private void AnnounceMatchEntryOnce()
    {
        if (matchFlow.AnnouncedEntered)
        {
            return;
        }

        LogDiagnostic("entered PvP match; waiting for portraits + gate (ContentTimeLeft)");
        ExecuteGameCommand(GameCommands.NavStop);
        matchFlow.AnnouncedEntered = true;
        SetPhase(AutoPhase.InMatch);
    }

    private async Task TickIntroBand(int timeLeftSeconds)
    {
        matchFlow.SawIntroBand = true;
        if (!matchFlow.AnnouncedPortrait)
        {
            LogDiagnostic("intro/portraits phase detected (timer ~31s)");
            matchFlow.AnnouncedPortrait = true;
        }

        movement.HaltPathing();
        greeting.TickIntro(timeLeftSeconds, settings);

        await NextFrame(PortraitPhasePollMs);
    }

    private async Task<bool> TryStartLiveMatch(int timeLeftSeconds)
    {
        var gateOpen = timeLeftSeconds > GateOpenSec && (matchFlow.SawIntroBand || matchFlow.TimerMovedFromBaseline);
        if (!gateOpen)
        {
            return false;
        }

        LogDiagnostic($"gate open detected by ContentTimeLeft -> {timeLeftSeconds}");
        matchFlow.InMatchLive = true;
        await EnableRotationAtMatchStart();
        return true;
    }

    private async Task EnableRotationAtMatchStart()
    {
        if (!rotation.Managed)
        {
            rotation.MarkRotationEnabled();
            return;
        }

        if (rotation.UsesLowHpPreset)
        {
            ExecuteGameCommand(GameCommands.AddLowHpTargeting);
            await NextFrame(PollMs);
        }
        ExecuteGameCommand(rotation.EnableCommand);
        rotation.MarkRotationEnabled();
        LogDiagnostic("rotation enabled (match start)");
    }
}
