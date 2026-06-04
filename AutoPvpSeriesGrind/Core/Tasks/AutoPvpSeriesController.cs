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

        var s = new SessionStats();
        s.CaptureJob();
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
        // Keep the session live while a "Then" action runs so the Finishing card still shows its stats;
        // the after-action's OnCompleted clears it. Otherwise drop it now, as before.
        if (s == session && Phase != AutoPhase.Finishing) session = null;
    }

    // Post-goal "Then" action (Return to inn / Logout / Close game). Gated on an actual goal completion and
    // dispatched at most once; manual Stop and faults never reach here.
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
            if (s == session) session = null;
        });
    }

    // Records a finished session to history exactly once (idempotent via Recorded). Never touches control
    // flow, so a history failure can't wedge automation.
    private void FinalizeRun(SessionStats? s)
    {
        if (s is null || s.Recorded) return;
        s.Recorded = true;
        if (s.MatchesCompleted == 0) return;
        try
        {
            Plugin.Instance.History.Append(new RunRecord
            {
                StartedAtUtc = s.StartedAt,
                EndedAtUtc = DateTime.UtcNow,
                DurationSeconds = s.Elapsed.TotalSeconds,
                MatchesCompleted = s.MatchesCompleted,
                Deaths = s.Deaths,
                JobId = s.JobId,
                JobAbbr = s.JobAbbr,
            });
            Diag($"Run recorded: {s.MatchesCompleted} matches, {s.Deaths} deaths over {s.Elapsed:hh\\:mm\\:ss}.");
        }
        catch (Exception ex)
        {
            Diag($"FinalizeRun failed to record history: {ex.Message}");
        }
    }

}
