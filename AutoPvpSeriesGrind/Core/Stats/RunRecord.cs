namespace AutoPvpSeriesGrind.Core.Stats;

[Serializable]
public sealed class RunRecord
{
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public double DurationSeconds { get; set; }

    public int MatchesCompleted { get; set; }
    public long SeriesExpGained { get; set; }

    public uint JobId { get; set; }
    public string JobAbbr { get; set; } = "";

    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
    public double MatchesPerHour => DurationSeconds > 0 ? MatchesCompleted / (DurationSeconds / 3600.0) : 0;
}
