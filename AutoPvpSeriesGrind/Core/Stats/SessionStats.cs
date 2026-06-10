using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Modes;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.Stats;

public sealed class SessionStats
{
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    public int MatchesCompleted { get; set; }

    public long SeriesExpStart { get; private set; }

    public long SeriesExpGained
    {
        get
        {
            var now = PvpProfileReader.SeriesTotalExperience();
            return now > SeriesExpStart ? now - SeriesExpStart : 0;
        }
    }

    public bool CompletedByGoal { get; set; }
    public bool Recorded { get; set; }
    public bool AfterActionDispatched { get; set; }

    public uint JobId { get; private set; }
    public string JobAbbr { get; private set; } = "";

    public TimeSpan Elapsed => DateTime.UtcNow - StartedAt;

    public ModeContext ToModeContext() => new()
    {
        MatchesCompleted = MatchesCompleted,
        Elapsed = Elapsed,
        SeriesRank = PvpProfileReader.SeriesCurrentRank(),
    };

    public void CaptureJob()
    {
        var me = Svc.Objects.LocalPlayer;
        if (me is null) return;
        JobId = me.ClassJob.RowId;
        JobAbbr = me.ClassJob.ValueNullable?.Abbreviation.ExtractText() ?? "";
    }

    public void CaptureSeriesBaseline()
        => SeriesExpStart = PvpProfileReader.SeriesTotalExperience();
}
