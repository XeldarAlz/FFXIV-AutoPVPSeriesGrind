using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Stats;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Numerics;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries(SessionStats session) : AutoCommon
{
    private readonly SessionStats session = session;
    private static readonly Random rng = HumanTiming.Rng;

    private RunSettings settings;
    private readonly PvpBrain brain = new(PvpStrategy.Moderate);

    private long nextQueueAllowedAtMs;
    private int matchesSinceBreak;
    private bool onBreak;

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
    private bool hasEnabledRotationThisLife;
    private bool clearedSignThisLife;

    private Vector3 lastMoveDest;
    private long lastMoveAtMs;
    private MoveKind? lastPlanKind;

    private bool wasDead;
    private long deadSinceMs;
    private bool rotationNeedsReset;

    private bool stopAfterCurrentMatch;

    private const int PollMs = 100;
    private const int MainLoopIdleMs = 500;
    private const int LiveTickMs = 150;
    private const int DutyCommencedSettleMs = 1000;
    // Grace period after death before re-applying the rotation, so it lands after the respawn completes.
    private const int RespawnRotationDelayMs = 10_000;

    private static bool InDuty() => Svc.Condition[ConditionFlag.BoundByDuty];

    private static bool IsDead()
        => Svc.Condition[ConditionFlag.Unconscious]
        || (Svc.Objects.LocalPlayer is { } me && me.MaxHp > 0 && me.CurrentHp == 0);

    private static bool IsNormal() => Svc.Condition[ConditionFlag.NormalConditions];

    private static void Cmd(string command) => Chat.ExecuteCommand(command);

    private static unsafe bool ResultsScreenVisible()
        => GenericHelpers.TryGetAddonByName<AtkUnitBase>(ApsgConstants.AddonNames.MatchResults, out var a)
        && GenericHelpers.IsAddonReady(a);

    protected override async Task Execute()
    {
        var cfg = Plugin.Cfg;
        settings = RunSettings.From(cfg);
        brain.SetStrategy(cfg.Strategy, cfg.CustomStrategy);

        ApsgLog.Chat($"Starting PvP Series grind ({Plugin.Cfg.ActiveMode.DisplayName}).");

        await Startup();

        while (!CancelToken.IsCancellationRequested)
        {
            if (InDuty())
            {
                if (await TryHandleMatchEnd()) continue;
            }
            else
            {
                if (TryCommenceDuty())
                {
                    Diag("duty ready popup -> commenced");
                    await NextFrame(DutyCommencedSettleMs);
                    continue;
                }

                if (await TickOutOfDuty()) return;
                continue;
            }

            if (!baselineCaptured)
                await CaptureBaseline();

            await RunWaitingPhase();

            if (inMatchLive)
                await TickLiveMatch();

            await NextFrame(inMatchLive ? LiveTickMs : MainLoopIdleMs);
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
        hasEnabledRotationThisLife = false;
        clearedSignThisLife = false;
        lastMoveDest = default;
        lastMoveAtMs = 0;
        lastPlanKind = null;
        brain.Reset();
        BrainTelemetry.Clear();
        wasDead = false;
        deadSinceMs = 0;
        rotationNeedsReset = false;
        Diag($"reset: {reason}");
    }

    private void CheckDeathAndReapplyRotation()
    {
        if (IsDead())
        {
            if (!wasDead)
            {
                wasDead = true;
                deadSinceMs = Environment.TickCount64;
                rotationNeedsReset = true;
                clearedSignThisLife = false;
                brain.Reset();
                Diag("death detected -> rotation will be re-applied after respawn");
            }
            return;
        }

        if (wasDead)
        {
            wasDead = false;
            if (rotationNeedsReset && Environment.TickCount64 - deadSinceMs >= RespawnRotationDelayMs && IsNormal())
            {
                Cmd(GameCommands.EnableRotation);
                rotationNeedsReset = false;
                Diag("respawn detected -> rotation re-applied");
            }
        }

        if (rotationNeedsReset && IsNormal())
        {
            Cmd(GameCommands.EnableRotation);
            rotationNeedsReset = false;
            Diag("rotation re-applied (failsafe)");
        }
    }
}
