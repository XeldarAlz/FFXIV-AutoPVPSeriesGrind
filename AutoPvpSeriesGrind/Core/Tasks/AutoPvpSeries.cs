using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Stats;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

// Faithful port of the "Casual Match PVP" SND script: queue the casual roulette, ride out the match while
// RotationSolver + vnavmesh fight on the crystal, fire the job Limit Break, send quickchat, leave on the
// results screen, then requeue — stopping after the configured match limit.
public sealed partial class AutoPvpSeries(SessionStats session) : AutoCommon
{
    private readonly SessionStats session = session;
    private static readonly Random rng = new();

    // Snapshot of the config at start, so changing settings mid-run can't tear the loop.
    private bool sendHello;
    private bool sendGoodMatch;

    // Per-duty match state.
    private bool inMatchLive;
    private bool baselineCaptured;
    private int dutyBaselineTime;
    private bool sawIntroBand;
    private bool timerMovedFromBaseline;
    private bool announcedEntered;
    private bool announcedPortrait;
    private int portraitHelloThreshold;
    private bool portraitHelloSent;
    private bool goodMatchSent;
    private bool ranSafetyMoveThisDuty;
    private int lbTick;
    private bool hasEnabledRotationThisLife;

    // Death tracking — RotationSolver disables itself on death, so we re-arm it after respawn.
    private bool wasDead;
    private long deadSinceMs;
    private bool rotationNeedsReset;

    private bool stopAfterCurrentMatch;

    private const int PollMs = 100;

    private static bool InDuty() => Svc.Condition[ConditionFlag.BoundByDuty];
    private static bool IsDead() => Svc.Condition[ConditionFlag.Unconscious];
    private static bool IsNormal() => Svc.Condition[ConditionFlag.NormalConditions];

    private static void Cmd(string command) => Chat.ExecuteCommand(command);

    private static unsafe bool ResultsScreenVisible()
        => GenericHelpers.TryGetAddonByName<AtkUnitBase>(ApsgConstants.AddonNames.MatchResults, out var a)
        && GenericHelpers.IsAddonReady(a);

    protected override async Task Execute()
    {
        var cfg = Plugin.Cfg;
        sendHello = cfg.SendHelloOnEntry;
        sendGoodMatch = cfg.SendGoodMatchOnResults;

        Svc.Chat.Print($"{ApsgConstants.LogPrefix} Starting PvP Series grind ({Plugin.Cfg.ActiveMode.DisplayName}).");

        await Startup();

        while (!CancelToken.IsCancellationRequested)
        {
            if (InDuty())
            {
                if (await TryHandleMatchEnd()) continue;
            }
            else
            {
                // Catch the "Duty Ready" popup the instant it appears, on any cycle — before the queue
                // branch's own poll — so a match pop is always commenced and never times out in queue.
                if (TryCommenceDuty())
                {
                    Diag("duty ready popup -> commenced");
                    await NextFrame(1000);
                    continue;
                }

                if (await TickOutOfDuty()) return; // stopped on match limit
                continue;
            }

            if (!baselineCaptured)
                await CaptureBaseline();

            await RunWaitingPhase();

            if (inMatchLive)
                await TickLiveMatch();

            await NextFrame(500);
        }
    }

    private void ResetDutyState(string reason)
    {
        inMatchLive = false;
        baselineCaptured = false;
        sawIntroBand = false;
        timerMovedFromBaseline = false;
        announcedEntered = false;
        announcedPortrait = false;
        portraitHelloSent = false;
        goodMatchSent = false;
        ranSafetyMoveThisDuty = false;
        lbTick = 0;
        hasEnabledRotationThisLife = false;
        wasDead = false;
        deadSinceMs = 0;
        rotationNeedsReset = false;
        Diag($"reset: {reason}");
    }

    // RotationSolver drops its rotation on death; re-arm "/rotation auto LowHP" once we're up again.
    private void CheckDeathAndReapplyRotation()
    {
        if (IsDead())
        {
            if (!wasDead)
            {
                wasDead = true;
                deadSinceMs = Environment.TickCount64;
                rotationNeedsReset = true;
                session.Deaths++;
                Diag("death detected -> rotation will be re-applied after respawn");
            }
            return;
        }

        if (wasDead)
        {
            wasDead = false;
            if (rotationNeedsReset && Environment.TickCount64 - deadSinceMs >= 10_000 && IsNormal())
            {
                Cmd("/rotation auto LowHP");
                rotationNeedsReset = false;
                Diag("respawn detected -> rotation re-applied");
            }
        }

        if (rotationNeedsReset && IsNormal())
        {
            Cmd("/rotation auto LowHP");
            rotationNeedsReset = false;
            Diag("rotation re-applied (failsafe)");
        }
    }
}
