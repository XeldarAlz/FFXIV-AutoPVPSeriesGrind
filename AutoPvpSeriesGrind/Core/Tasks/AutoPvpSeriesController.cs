using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Stats;
using clib.Services;
using ECommons.Automation;

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
        if (s == session) session = null;
        Phase = AutoPhase.Idle;
        MaybeRunFollowUp(s);
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

    // Optional command on a match-limit stop (the script's "Follow-up script", repurposed as a chat command).
    private static void MaybeRunFollowUp(SessionStats s)
    {
        if (!s.CompletedByGoal) return;
        var cmd = Plugin.Cfg.FollowUpCommand?.Trim();
        if (string.IsNullOrEmpty(cmd)) return;
        Diag($"Goal reached; running follow-up command -> {cmd}");
        try { Chat.ExecuteCommand(cmd); }
        catch (Exception ex) { Diag($"Follow-up command failed: {ex.Message}"); }
    }
}
