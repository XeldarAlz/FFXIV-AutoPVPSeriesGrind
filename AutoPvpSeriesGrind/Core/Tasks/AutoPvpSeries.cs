using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Stats;
using ECommons;
using ECommons.Automation;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries : AutoCommon
{
    private readonly SessionStats session;

    private RunSettings settings;
    private readonly PvpBrain brain = new(PvpStrategy.Moderate);
    private readonly MovementExecutor movement = new();
    private readonly RotationController rotation;
    private readonly GreetingDirector greeting = new();

    public AutoPvpSeries(SessionStats session)
    {
        this.session = session;
        rotation = new RotationController(brain);
    }

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
    private bool ranSafetyMoveThisDuty;

    private bool stopAfterCurrentMatch;

    private const int PollMs = 100;
    private const int MainLoopIdleMs = 500;
    private const int LiveTickMs = 150;
    private const int DutyCommencedSettleMs = 1000;

    private static bool InDuty() => MatchState.InDuty();

    private static bool IsDead() => MatchState.LocalIsDead();

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
        ResetMatchFlow();
        movement.Reset();
        rotation.Reset();
        greeting.Reset();
        brain.Reset();
        BrainTelemetry.Clear();
        Diag($"reset: {reason}");
    }

    private void ResetMatchFlow()
    {
        inMatchLive = false;
        baselineCaptured = false;
        dutyBaselineTime = 0;
        sawIntroBand = false;
        timerMovedFromBaseline = false;
        announcedEntered = false;
        announcedPortrait = false;
        ranSafetyMoveThisDuty = false;
    }
}
