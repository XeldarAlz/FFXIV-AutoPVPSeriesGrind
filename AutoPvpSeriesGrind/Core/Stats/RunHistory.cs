using Newtonsoft.Json;
using System.IO;

namespace AutoPvpSeriesGrind.Core.Stats;

internal sealed class RunHistory
{
    private const int MaxRecords = 500;
    private const string FileName = "run-history.json";
    private const double SecondsPerHour = 3600.0;

    public List<RunRecord> Records { get; private set; } = [];

    public LifetimeTotals Lifetime { get; private set; }

    public RunHistory()
    {
        Load();
        RecomputeLifetime();
    }

    private static string FilePath
        => Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, FileName);

    private void Load()
    {
        try
        {
            var path = FilePath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var records = JsonConvert.DeserializeObject<List<RunRecord>>(json);
                if (records is not null) Records = records;
            }
        }
        catch (Exception ex)
        {
            ApsgLog.Warn(ex, "RunHistory load failed; starting empty");
        }
    }

    public void Append(RunRecord record)
    {
        Records.Add(record);
        Records.Sort((a, b) => b.EndedAtUtc.CompareTo(a.EndedAtUtc));
        if (Records.Count > MaxRecords)
            Records.RemoveRange(MaxRecords, Records.Count - MaxRecords);
        RecomputeLifetime();
        Save();
    }

    public void Clear()
    {
        Records.Clear();
        RecomputeLifetime();
        Save();
    }

    private void RecomputeLifetime()
    {
        var matches = 0;
        long seriesExp = 0;
        double seconds = 0;
        for (var recordIndex = 0; recordIndex < Records.Count; recordIndex++)
        {
            var record = Records[recordIndex];
            matches += record.MatchesCompleted;
            seriesExp += record.SeriesExpGained;
            seconds += record.DurationSeconds;
        }
        Lifetime = new LifetimeTotals
        {
            Runs = Records.Count,
            Matches = matches,
            SeriesExp = seriesExp,
            Seconds = seconds,
        };
    }

    private void Save()
    {
        try
        {
            var dir = Plugin.PluginInterface.ConfigDirectory;
            if (!dir.Exists) dir.Create();
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(Records, Formatting.Indented));
        }
        catch (Exception ex)
        {
            ApsgLog.Warn(ex, "RunHistory save failed");
        }
    }

    public struct LifetimeTotals
    {
        public int Runs;
        public int Matches;
        public long SeriesExp;
        public double Seconds;

        public readonly double MatchesPerHour => Seconds > 0 ? Matches / (Seconds / SecondsPerHour) : 0;
        public readonly TimeSpan Duration => TimeSpan.FromSeconds(Seconds);
    }
}
