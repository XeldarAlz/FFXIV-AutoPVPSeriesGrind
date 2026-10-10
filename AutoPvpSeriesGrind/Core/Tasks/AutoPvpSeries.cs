using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Humanize;
using AutoPvpSeriesGrind.Core.Rotation;
using AutoPvpSeriesGrind.Core.Stats;
using ECommons.Automation;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries : AutoCommon
{
    private readonly SessionStats session;

    private RunSettings settings;
    private readonly PvpBrain brain = new(PvpStrategy.Moderate);
    private readonly FrontlineBrain frontline = new();
    private readonly ManualBrain manual = new();
    private bool playerSteering;
    private MatchType matchType;
    private readonly MovementExecutor movement = new();
    private readonly RotationController rotation;
    private readonly GreetingDirector greeting = new();
    private SelfSignClearer signClearer;
    private readonly Action holdStill;
    private Persona persona = Persona.Neutral;

    public AutoPvpSeries(SessionStats session)
    {
        this.session = session;
        rotation = new RotationController(brain);
        holdStill = movement.Stop;
        brain.CanSee = LineOfSight.IsVisible;
        brain.Reachable = movement.IsReachable;
    }

    private long nextQueueAllowedAtMs;
    private int matchesSinceBreak;
    private bool onBreak;
    private bool queuePenaltyAnnounced;

    private MatchFlowState matchFlow;

    private bool stopAfterCurrentMatch;
    private bool stopRequested;

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
        public long LeaveSpawnNotBeforeMs;
        public bool AwaitingRespawnExit;
        public int GateApproachStartSec;
        public int GateApproachDelayMs;
        public long GateApproachArmedAtMs;
        public Vector3? GateTarget;
        public TeamBases? Bases;
        public bool FrontlineBaseChecked;
        public Vector3? FrontlineDeathSpot;

        public void Reset() => this = default;
    }

    private static bool InDuty() => MatchState.InDuty();

    private static bool IsDead() => MatchState.LocalIsDead();

    private static void ExecuteGameCommand(string command) => Chat.ExecuteCommand(command);

    private static bool ResultsScreenVisible()
        => AddonProbe.IsReady(ApsgConstants.AddonNames.MatchResults) || AddonProbe.IsReady(ApsgConstants.AddonNames.FrontlineResults);

    private void SetPhase(AutoPhase phase) => Plugin.Instance.Controller.Phase = phase;

    public bool StopRequested => stopRequested;

    public void SetStopRequested(bool value)
    {
        if (stopRequested == value)
        {
            return;
        }

        stopRequested = value;
        LogDiagnostic(value
            ? "stop requested -> nothing new is queued; the run ends once out of duty and out of the queue"
            : "stop request withdrawn -> queueing continues");
    }

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

        ApsgChat.Print($"Starting PvP Series grind ({matchType}, {cfg.ActiveMode.DisplayName}).");

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
        manual.Reset();
        playerSteering = false;
        BrainTelemetry.Clear();
        MatchRecorder.End();
        LogDiagnostic($"reset: {reason}");
    }

    private void ResetMatchFlow() => matchFlow.Reset();

    private void RollPersona()
    {
        persona = PersonaRoller.Roll(settings.Humanize);
        brain.SetPersona(in persona);
        frontline.SetPersona(in persona);
        movement.SetPersona(in persona);
        if (settings.Humanize != HumanizeLevel.Off)
        {
            LogDiagnostic($"play style for this match -> {persona.Describe()}");
        }
    }

    private async Task ReactToPostureChange(bool changed)
    {
        if (!changed)
        {
            return;
        }

        var reactionMs = persona.NextReactionMs();
        if (reactionMs > 0)
        {
            await DelayMs(reactionMs);
        }
    }
}
