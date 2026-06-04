using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.Stats;

// Live state for the current grind session. Mutated by the running task, read by the UI, finalized into a
// RunRecord by the controller.
public sealed class SessionStats
{
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public int MatchesCompleted { get; set; }
    public int Deaths { get; set; }

    // Set true when the run's stop condition (mode goal) was met, vs a manual Stop; gates the follow-up command.
    public bool CompletedByGoal { get; set; }
    public bool Recorded { get; set; }

    public uint JobId { get; private set; }
    public string JobAbbr { get; private set; } = "";

    public TimeSpan Elapsed => DateTime.UtcNow - StartedAt;

    public Core.Modes.ModeContext ToModeContext() => new()
    {
        MatchesCompleted = MatchesCompleted,
        Elapsed = Elapsed,
        SeriesRank = Core.Game.PvpProfileReader.SeriesCurrentRank(),
    };

    public void CaptureJob()
    {
        var me = Svc.Objects.LocalPlayer;
        if (me is null) return;
        JobId = me.ClassJob.RowId;
        JobAbbr = me.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? "";
    }
}
