using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Stats;
using clib.Services;

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
        => lastResultTick != NoResult && unchecked(Environment.TickCount - lastResultTick) is var age && age >= 0 && age < withinMs;
    public void ClearLastResult() => lastResultTick = NoResult;

    private static void Diag(string message)
        => ECommons.DalamudServices.Svc.Log.Info($"{ApsgConstants.LogPrefix} {message}");

    public void Start()
    {
        if (Running) return;

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            var missing = string.Join(", ", ExternalPlugins.All
                .Where(p => ExternalPlugins.Catalog[p].Required && !ExternalPlugins.IsInstalled(p))
                .Select(p => ExternalPlugins.Catalog[p].DisplayName));
            Diag($"Start aborted: required plugins missing ({missing}).");
            ECommons.DalamudServices.Svc.Chat.PrintError($"{ApsgConstants.LogPrefix} Cannot start — install all required plugins first: {missing}.");
            return;
        }

        ClearLastResult();
        var s = new SessionStats();
        s.CaptureJob();
        s.CaptureSeriesBaseline();
        session = s;
        Phase = AutoPhase.Queueing;
        Diag($"Run starting: mode {Plugin.Cfg.ActiveMode.DisplayName}.");

        Svc.Automation.Start(new AutoPvpSeries(s), OnCompleted: () => EndRun(s));
    }

    public void Stop()
    {
        var ending = session;
        Svc.Automation.Stop();
        FinalizeRun(ending);
        session = null;
        Phase = AutoPhase.Idle;
        if (ending is not null) Diag("Stop requested; session cleared.");
    }

    private void EndRun(SessionStats s)
    {
        FinalizeRun(s);
        Phase = AutoPhase.Idle;
        MaybeRunAfterAction(s);
        if (s == session && Phase != AutoPhase.Finishing) session = null;
    }

    private void MaybeRunAfterAction(SessionStats s)
    {
        if (!s.CompletedByGoal || s.AfterActionDispatched) return;
        s.AfterActionDispatched = true;

        var action = Plugin.Cfg.AfterRun;
        if (action == AfterRunAction.StayLoggedIn)
        {
            Diag("Goal reached; after-run action = stay where you are (no-op).");
            return;
        }

        Diag($"Goal reached; starting after-run action {action}.");
        Phase = AutoPhase.Finishing;
        Svc.Automation.Start(new AutoAfterRun(action), OnCompleted: () =>
        {
            Diag($"After-run action {action} finished.");
            Phase = AutoPhase.Idle;
            ClearLastResult();
            if (s == session) session = null;
        });
    }

    private void FinalizeRun(SessionStats? s)
    {
        if (s is null || s.Recorded) return;
        s.Recorded = true;
        if (s.MatchesCompleted == 0) return;

        LastMatches = s.MatchesCompleted;
        LastSeriesExp = s.SeriesExpGained;
        LastByGoal = s.CompletedByGoal;
        lastResultTick = Environment.TickCount;

        try
        {
            Plugin.Instance.History.Append(new RunRecord
            {
                StartedAtUtc = s.StartedAt,
                EndedAtUtc = DateTime.UtcNow,
                DurationSeconds = s.Elapsed.TotalSeconds,
                MatchesCompleted = s.MatchesCompleted,
                SeriesExpGained = s.SeriesExpGained,
                JobId = s.JobId,
                JobAbbr = s.JobAbbr,
            });
            Diag($"Run recorded: {s.MatchesCompleted} matches, {s.SeriesExpGained} Series EXP over {s.Elapsed:hh\\:mm\\:ss}.");
        }
        catch (Exception ex)
        {
            Diag($"FinalizeRun failed to record history: {ex.Message}");
        }
    }

}
