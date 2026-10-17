using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Humanize;
using AutoPvpSeriesGrind.Core.Ipc;
using ECommons.DalamudServices;
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
    private const int GateApproachJitterReactions = 2;
    private const int MillisecondsPerSecond = 1000;

    private async Task<bool> TryHandleMatchEnd()
    {
        if (!completion.Active || !InExpectedDuty())
        {
            return false;
        }

        var resultsVisible = ResultsScreenVisible();
        if (resultsVisible) completion.Confirm(Svc.ClientState.TerritoryType);
        if (!completion.Confirmed) return false;
        completionObservedAt ??= Environment.TickCount64;
        movement.Stop();
        CompleteMatch(resultsVisible ? "results screen visible" : "duty completion event");
        if (!resultsVisible && Environment.TickCount64 - completionObservedAt.Value < 15_000)
        {
            Status = "Match completed; waiting for results";
            await DelayMs(MainLoopIdleMs);
            return true;
        }

        await LingerThenLeave();
        if (!InDuty()) ResetDutyState("left duty (post-match)");
        return true;
    }

    private void CompleteMatch(string signal)
    {
        if (!completion.TryCount()) return;
        session.MatchesCompleted++;
        LogDiagnostic($"completed match {session.MatchesCompleted} ({signal})");

        EvaluateStopCondition();
        ScheduleNextQueue();
    }

    private void EvaluateStopCondition()
    {
        if (stopAfterCurrentMatch || !Plugin.Cfg.ActiveMode.IsComplete(session.ToModeContext()))
        {
            return;
        }

        stopAfterCurrentMatch = true;
        session.CompletedByGoal = true;
        LogDiagnostic($"stop condition met ({Plugin.Cfg.ActiveMode.DisplayName}) -> ending the run once out of duty");
    }

    // The leave delay is how long we sit on the results screen before bailing. "Good Match" is sent only if
    // its random delay lands inside that window; a short leave delay means we just leave early without it.
    private async Task LingerThenLeave()
    {
        var lingerMs = Math.Max(0, settings.LeaveDutyDelayMs);
        var waitedMs = 0;

        if (ResultsScreenVisible() && greeting.PlanGoodMatchDelayMs(settings) is { } goodbyeAtMs)
        {
            if (goodbyeAtMs <= lingerMs)
            {
                if (goodbyeAtMs > 0) await DelayMs(goodbyeAtMs);
                ExecuteGameCommand(GameText.QuickChatGoodMatch());
                await DelayMs(PostQuickChatMs);
                waitedMs = goodbyeAtMs + PostQuickChatMs;
            }
            else
            {
                LogDiagnostic($"leaving early -> skipped \"Good Match\" (delay {goodbyeAtMs}ms > leave delay {lingerMs}ms)");
            }
        }

        if (waitedMs < lingerMs)
            await DelayMs(lingerMs - waitedMs);

        LogDiagnostic("match completion confirmed -> leaving duty");
        ExecuteGameCommand(GameCommands.NavStop);
        DutyOps.LeaveCurrentContent();
        if (!await WaitUntilTimed(() => !InDuty(), LeaveDutyTimeoutMs, "left-duty", checkMs: PollMs))
            throw new InvalidOperationException("The completed duty did not exit. Run stopped; leave the duty manually.");
    }

    private async Task CaptureBaseline()
    {
        ExecuteGameCommand(GameCommands.NavStop);
        LogDiagnostic("in duty -> waiting for PvP area before baseline capture");
        if (!await WaitUntilTimed(InExpectedDuty, PvpAreaWaitMs, "selected-pvp-duty", checkMs: PollMs))
            throw new InvalidOperationException("The selected PvP duty was not recognized. Run stopped.");

        ResetMatchFlow();
        completion.Begin(Svc.ClientState.TerritoryType);
        queueSubmission.Reset();
        matchFlow.DutyBaselineTime = DutyOps.ContentTimeLeft();
        matchFlow.BaselineCaptured = true;
        RollPersona();
        greeting.PrepareForMatch(settings);
        SetPhase(AutoPhase.InMatch);

        LogDiagnostic($"duty entry baseline ContentTimeLeft -> {matchFlow.DutyBaselineTime}");
    }

    private async Task RunWaitingPhase()
    {
        if (matchType.IsLargeScale())
        {
            if (matchType == MatchType.RivalWings && !Svc.DutyState.IsDutyStarted)
            {
                if (!matchFlow.FrontlineBaseChecked && MatchState.PlayerPosition() is { } spawn)
                {
                    TeamBrain.MarkBase(spawn);
                    matchFlow.FrontlineBaseChecked = true;
                }
                movement.Stop();
                Status = "Waiting for the Rival Wings match to start";
                return;
            }
            if (!matchFlow.InMatchLive && InDuty())
            {
                AnnounceMatchEntryOnce();
                LogDiagnostic($"{matchType}: following the team");
                BeginLiveMatch();
            }
            return;
        }

        while (InExpectedDuty() && !matchFlow.InMatchLive && !completion.Confirmed && !ResultsScreenVisible() && !CancelToken.IsCancellationRequested)
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

                await DelayMs(PollMs);
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
            PlanGateApproach();
        }

        var territory = Svc.ClientState.TerritoryType;
        CaptureBasesAtSpawn(territory);

        if (!Plugin.Cfg.ManualMovement && timeLeftSeconds <= matchFlow.GateApproachStartSec && GateApproachDue())
        {
            ApproachGate(territory);
        }
        else
        {
            movement.HaltPathing();
        }

        greeting.TickIntro(timeLeftSeconds, settings);

        await DelayMs(PortraitPhasePollMs);
    }

    private void PlanGateApproach()
    {
        var late = persona.Gate == GateStyle.Late;
        matchFlow.GateApproachStartSec = late ? 0 : Math.Max(1, (int)MathF.Ceiling(persona.GateSeconds));
        matchFlow.GateApproachDelayMs = late ? 0 : persona.NextReactionMs() * GateApproachJitterReactions;
        LogDiagnostic(late
            ? $"gate plan: leave spawn {persona.GateSeconds:F1}s after it opens"
            : $"gate plan: move up at {matchFlow.GateApproachStartSec}s left (+{matchFlow.GateApproachDelayMs}ms)");
    }

    private bool GateApproachDue()
    {
        var now = Environment.TickCount64;
        if (matchFlow.GateApproachArmedAtMs == 0)
        {
            matchFlow.GateApproachArmedAtMs = now;
        }
        return now - matchFlow.GateApproachArmedAtMs >= matchFlow.GateApproachDelayMs;
    }

    private async Task<bool> TryStartLiveMatch(int timeLeftSeconds)
    {
        var gateOpen = timeLeftSeconds > GateOpenSec && (matchFlow.SawIntroBand || matchFlow.TimerMovedFromBaseline);
        if (!gateOpen)
        {
            return false;
        }

        LogDiagnostic($"gate open detected by ContentTimeLeft -> {timeLeftSeconds}");
        WarnIfNavmeshNotReady();
        if (persona.Gate == GateStyle.Late)
        {
            matchFlow.LeaveSpawnNotBeforeMs = Environment.TickCount64 + (long)(persona.GateSeconds * MillisecondsPerSecond);
        }
        BeginLiveMatch();
        return true;
    }

    private void BeginLiveMatch()
    {
        Status = string.Empty;
        matchFlow.InMatchLive = true;
        if (settings.RecordMatches)
        {
            MatchRecorder.Begin(Svc.ClientState.TerritoryType);
        }
        rotation.OnMatchStart();
    }

    private void WarnIfNavmeshNotReady()
    {
        var nav = NavIpc.Instance;
        if (nav.IsReady())
        {
            return;
        }

        Warn($"navmesh not ready at gate open (build progress {nav.BuildProgress():F2}) -> movement stalls until vnavmesh finishes this zone");
    }
}
