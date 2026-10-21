using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Modes;
using AutoPvpSeriesGrind.Core.Shopping;
using AutoPvpSeriesGrind.Core.Stats;
using clib.Services;
using System.Text;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal enum AutoPhase { Idle, Queueing, InMatch, Shopping, Finishing }

internal sealed class AutoPvpSeriesController
{
    public bool Running => Svc.Automation.Running;
    private string lastStopReason = string.Empty;
    public string Status => Svc.Automation.CurrentTask?.Status ?? (lastStopReason.Length > 0 ? lastStopReason : "Idle");
    public AutoPhase Phase { get; set; } = AutoPhase.Idle;

    private SessionStats? session;
    public SessionStats? SessionSnapshot => session;

    private AutoPvpSeries? series;
    public bool StopAfterMatchAvailable => series is not null;
    public bool StopAfterMatchRequested => series?.StopRequested ?? false;

    public int LastMatches { get; private set; }
    public long LastSeriesExp { get; private set; }
    public bool LastByGoal { get; private set; }
    private const int NoResult = int.MinValue / 2;
    private int lastResultTick = NoResult;

    public bool HasRecentResult(int withinMs)
    {
        if (lastResultTick == NoResult)
        {
            return false;
        }

        var elapsedMs = unchecked(Environment.TickCount - lastResultTick);
        return elapsedMs >= 0 && elapsedMs < withinMs;
    }

    public void ClearLastResult() => lastResultTick = NoResult;

    private static void LogDiagnostic(string message) => RunLog.Info(message);

    public void Start()
    {
        if (Running) return;

        if (!Enum.IsDefined(Plugin.Cfg.MatchType) || !ECommons.DalamudServices.Svc.ClientState.IsLoggedIn
            || (MatchState.InDuty() ? !PvpDuty.Contains(Plugin.Cfg.MatchType, ECommons.DalamudServices.Svc.ClientState.TerritoryType) : DutyOps.HasDutyInFlight()))
        {
            ApsgChat.PrintError("Cannot start: settle the character and clear any unrelated queue or duty first.");
            return;
        }

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            var missing = MissingRequiredPluginNames();
            LogDiagnostic($"Start aborted: required plugins missing ({missing}).");
            ApsgChat.PrintError($"Cannot start. Install all required plugins first: {missing}.");
            return;
        }

        ClearLastResult();
        lastStopReason = string.Empty;
        LiftRankTargetAboveCurrentRank();
        var stats = new SessionStats();
        stats.CaptureJob();
        stats.CaptureSeriesBaseline();
        session = stats;
        Phase = AutoPhase.Queueing;
        LogDiagnostic($"Run starting: mode {Plugin.Cfg.ActiveMode.DisplayName}.");

        var task = new AutoPvpSeries(stats);
        series = task;
        Svc.Automation.Start(task, OnCompleted: () => EndRun(stats));
    }

    public void ToggleStopAfterMatch()
    {
        if (series is null)
        {
            return;
        }

        series.SetStopRequested(!series.StopRequested);
    }

    public void ShopNow()
    {
        if (Running)
        {
            return;
        }

        var skippedItemIds = new HashSet<uint>();
        var plan = ShoppingList.PlanTrip(ShopTrigger.Always, skippedItemIds);
        if (plan is null)
        {
            ApsgChat.Print(Loc.T(L.Shop.NothingToBuy));
            return;
        }

        LogDiagnostic("Shop now: starting a shopping trip.");
        Phase = AutoPhase.Shopping;
        Svc.Automation.Start(new ShopTrip(plan, skippedItemIds), OnCompleted: () => Phase = AutoPhase.Idle);
    }

    private static void LiftRankTargetAboveCurrentRank()
    {
        var cfg = Plugin.Cfg;
        if (cfg.ActiveMode.Id != SeriesRankMode.ModeId)
        {
            return;
        }

        var lowestOpenRank = SeriesRankMode.LowestOpenTarget(PvpProfileReader.SeriesCurrentRank());
        if (cfg.TargetSeriesRank >= lowestOpenRank)
        {
            return;
        }

        LogDiagnostic($"Rank target {cfg.TargetSeriesRank} is already reached -> raised to {lowestOpenRank}.");
        cfg.TargetSeriesRank = lowestOpenRank;
        cfg.SaveDebounced();
    }

    private static string MissingRequiredPluginNames()
    {
        var namesBuilder = new StringBuilder();
        for (var pluginIndex = 0; pluginIndex < ExternalPlugins.All.Count; pluginIndex++)
        {
            var plugin = ExternalPlugins.All[pluginIndex];
            var info = ExternalPlugins.Catalog[plugin];
            if (!ExternalPlugins.IsRequired(plugin) || ExternalPlugins.IsInstalled(plugin))
            {
                continue;
            }

            if (namesBuilder.Length > 0)
            {
                namesBuilder.Append(", ");
            }

            namesBuilder.Append(info.DisplayName);
        }

        return namesBuilder.ToString();
    }

    public void Stop()
    {
        var ending = session;
        if (ending is not null) ending.CompletedByGoal = false;
        series?.StopAutomation();
        Svc.Automation.Stop();
        FinalizeRun(ending);
        session = null;
        series = null;
        Phase = AutoPhase.Idle;
        if (ending is not null) LogDiagnostic("Stop requested; session cleared.");
    }

    private void EndRun(SessionStats stats)
    {
        series = null;
        FinalizeRun(stats);
        Phase = AutoPhase.Idle;
        MaybeRunAfterAction(stats);
        if (stats == session && Phase != AutoPhase.Finishing) session = null;
    }

    private void MaybeRunAfterAction(SessionStats stats)
    {
        if (!stats.CompletedByGoal || stats.AfterActionDispatched || stats.StopReason.Length > 0) return;
        stats.AfterActionDispatched = true;

        var action = Plugin.Cfg.AfterRun;
        var commandBlock = Plugin.Cfg.PostRunCommands;
        if (action == AfterRunAction.StayLoggedIn && string.IsNullOrWhiteSpace(commandBlock))
        {
            LogDiagnostic("Goal reached; after-run action = stay where you are (no-op).");
            return;
        }

        LogDiagnostic($"Goal reached; starting after-run action {action}.");
        Phase = AutoPhase.Finishing;
        Svc.Automation.Start(new AutoAfterRun(action, commandBlock), OnCompleted: () =>
        {
            LogDiagnostic($"After-run action {action} finished.");
            Phase = AutoPhase.Idle;
            ClearLastResult();
            if (stats == session) session = null;
        });
    }

    private void FinalizeRun(SessionStats? stats)
    {
        MatchRecorder.End();
        if (stats is null || stats.Recorded) return;
        stats.Recorded = true;
        lastStopReason = stats.StopReason;
        if (stats.MatchesCompleted == 0 && stats.UnconfirmedDepartures == 0 && stats.StopReason.Length == 0) return;

        RecordRun(stats);

        try
        {
            Plugin.Instance.History.Append(new RunRecord
            {
                StartedAtUtc = stats.StartedAt,
                EndedAtUtc = DateTime.UtcNow,
                DurationSeconds = stats.Elapsed.TotalSeconds,
                MatchesCompleted = stats.MatchesCompleted,
                UnconfirmedDepartures = stats.UnconfirmedDepartures,
                StopReason = stats.StopReason,
                SeriesExpGained = stats.SeriesExpGained,
                JobId = stats.JobId,
                JobAbbr = stats.JobAbbr,
            });
            LogDiagnostic($"Run recorded: {stats.MatchesCompleted} matches, {stats.SeriesExpGained} Series EXP over {stats.Elapsed:hh\\:mm\\:ss}.");
        }
        catch (Exception ex)
        {
            LogDiagnostic($"FinalizeRun failed to record history: {ex.Message}");
        }
    }

    private void RecordRun(SessionStats stats)
    {
        LastMatches = stats.MatchesCompleted;
        LastSeriesExp = stats.SeriesExpGained;
        LastByGoal = stats.CompletedByGoal;
        lastResultTick = Environment.TickCount;
    }
}
