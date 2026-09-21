using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Rotation;
using AutoPvpSeriesGrind.Core.Stats;
using ECommons.Automation;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries : AutoCommon
{
    private readonly SessionStats session;

    private RunSettings settings;
    private readonly PvpBrain brain = new(PvpStrategy.Moderate);
    private readonly FrontlineBrain frontline = new();
    private MatchType matchType;
    private readonly MovementExecutor movement = new();
    private readonly RotationController rotation;
    private readonly GreetingDirector greeting = new();
    private SelfSignClearer signClearer;
    private readonly Action holdStill;

    public AutoPvpSeries(SessionStats session)
    {
        this.session = session;
        rotation = new RotationController(brain);
        holdStill = movement.Stop;
        brain.CanSee = LineOfSight.IsVisible;
    }

    private long nextQueueAllowedAtMs;
    private int matchesSinceBreak;
    private bool onBreak;
    private bool queuePenaltyAnnounced;

    private MatchFlowState matchFlow;

    private bool stopAfterCurrentMatch;

    private const int PollMs = 100;
    private const int MainLoopIdleMs = 500;
    private const int LiveTickMs = 150;
    private const int DutyCommencedSettleMs = 1000;

    private struct MatchFlowState
    {
        public bool InMatchLive;
        public bool BaselineCaptured;
        public int DutyBaselineTime;
        public bool SawIntroBand;
        public bool TimerMovedFromBaseline;
        public bool AnnouncedEntered;
        public bool AnnouncedPortrait;
        public bool RanSafetyMoveThisDuty;
        public bool LoggedMissingObjective;
        public bool LeftSpawn;
        public long LeaveSpawnStartedAtMs;
        public int GateApproachStartSec;
        public TeamBases? Bases;

        public void Reset() => this = default;
    }

    private static bool InDuty() => MatchState.InDuty();

    private static bool IsDead() => MatchState.LocalIsDead();

    private static void ExecuteGameCommand(string command) => Chat.ExecuteCommand(command);

    private static bool ResultsScreenVisible()
        => AddonProbe.IsReady(ApsgConstants.AddonNames.MatchResults) || AddonProbe.IsReady(ApsgConstants.AddonNames.FrontlineResults);

    private void SetPhase(AutoPhase phase) => Plugin.Instance.Controller.Phase = phase;

    protected override async Task Execute()
    {
        var cfg = Plugin.Cfg;
        settings = RunSettings.From(cfg);
        matchType = cfg.MatchType;
        brain.SetStrategy(cfg.Strategy, cfg.CustomStrategy);
        frontline.SetStrategy(cfg.FrontlineStrategy, cfg.CustomFrontline);
        brain.OwnsTargeting = settings.BrainTargets;
        brain.Targeting = cfg.Targeting;
        rotation.Configure(cfg.RotationProvider == RotationProvider.Internal, RotationSettings.From(cfg));

        ApsgLog.Chat($"Starting PvP Series grind ({matchType}, {cfg.ActiveMode.DisplayName}).");

        await Startup();

        while (!CancelToken.IsCancellationRequested)
        {
            if (await TryHandleMatchEnd())
            {
                continue;
            }

            if (!InDuty())
            {
                if (await HandleOutOfDuty())
                {
                    return;
                }

                continue;
            }

            if (!matchFlow.BaselineCaptured)
            {
                await CaptureBaseline();
            }

            await RunWaitingPhase();

            if (matchFlow.InMatchLive)
            {
                await TickLiveMatch();
            }

            await DelayMs(matchFlow.InMatchLive ? LiveTickMs : MainLoopIdleMs);
        }
    }

    private async Task<bool> HandleOutOfDuty()
    {
        if (TryCommenceDuty())
        {
            LogDiagnostic("duty ready popup -> commenced");
            await DelayMs(DutyCommencedSettleMs);
            return false;
        }

        return await TickOutOfDuty();
    }

    private void ResetDutyState(string reason)
    {
        ResetMatchFlow();
        movement.Reset();
        rotation.Reset();
        greeting.Reset();
        signClearer.Reset();
        brain.Reset();
        frontline.Reset();
        BrainTelemetry.Clear();
        MatchRecorder.End();
        LogDiagnostic($"reset: {reason}");
    }

    private void ResetMatchFlow() => matchFlow.Reset();
}
