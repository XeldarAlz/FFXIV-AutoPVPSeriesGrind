using AutoPvpSeriesGrind.Core.Debug;
using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Stats;
using clib.Services;
using System.Text;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal enum AutoPhase { Idle, Queueing, InMatch, Finishing }

internal sealed class AutoPvpSeriesController
{
    public bool Running => Svc.Automation.Running;
    public string Status => Svc.Automation.CurrentTask?.Status ?? "Idle";
    public AutoPhase Phase { get; set; } = AutoPhase.Idle;

    private SessionStats? session;
    public SessionStats? SessionSnapshot => session;

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

    private static void LogDiagnostic(string message) => ApsgLog.Info(message);

    public void Start()
    {
        if (Running) return;

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            var missing = MissingRequiredPluginNames();
            LogDiagnostic($"Start aborted: required plugins missing ({missing}).");
            ApsgLog.ChatError($"Cannot start. Install all required plugins first: {missing}.");
            return;
        }

        ClearLastResult();
        var stats = new SessionStats();
        stats.CaptureJob();
        stats.CaptureSeriesBaseline();
        session = stats;
        Phase = AutoPhase.Queueing;
        LogDiagnostic($"Run starting: mode {Plugin.Cfg.ActiveMode.DisplayName}.");

        Svc.Automation.Start(new AutoPvpSeries(stats), OnCompleted: () => EndRun(stats));
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
        Svc.Automation.Stop();
        FinalizeRun(ending);
        session = null;
        Phase = AutoPhase.Idle;
        if (ending is not null) LogDiagnostic("Stop requested; session cleared.");
    }

    private void EndRun(SessionStats stats)
    {
        FinalizeRun(stats);
        Phase = AutoPhase.Idle;
        MaybeRunAfterAction(stats);
        if (stats == session && Phase != AutoPhase.Finishing) session = null;
    }

    private void MaybeRunAfterAction(SessionStats stats)
    {
        if (!stats.CompletedByGoal || stats.AfterActionDispatched) return;
        stats.AfterActionDispatched = true;

        var action = Plugin.Cfg.AfterRun;
        if (action == AfterRunAction.StayLoggedIn)
        {
            LogDiagnostic("Goal reached; after-run action = stay where you are (no-op).");
            return;
        }

        LogDiagnostic($"Goal reached; starting after-run action {action}.");
        Phase = AutoPhase.Finishing;
        Svc.Automation.Start(new AutoAfterRun(action), OnCompleted: () =>
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
        if (stats.MatchesCompleted == 0) return;

        RecordRun(stats);

        try
        {
            Plugin.Instance.History.Append(new RunRecord
            {
                StartedAtUtc = stats.StartedAt,
                EndedAtUtc = DateTime.UtcNow,
                DurationSeconds = stats.Elapsed.TotalSeconds,
                MatchesCompleted = stats.MatchesCompleted,
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
