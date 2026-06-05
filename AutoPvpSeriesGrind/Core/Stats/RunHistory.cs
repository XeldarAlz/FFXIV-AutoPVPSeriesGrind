using ECommons.DalamudServices;
using Newtonsoft.Json;
using System.IO;

namespace AutoPvpSeriesGrind.Core.Stats;

internal sealed class RunHistory
{
    private const int MaxRecords = 500;
    private const string FileName = "run-history.json";

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
            Svc.Log.Warning(ex, $"{ApsgConstants.LogPrefix} RunHistory load failed; starting empty");
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
        var totals = new LifetimeTotals { Runs = Records.Count };
        foreach (var r in Records)
        {
            totals.Matches += r.MatchesCompleted;
            totals.SeriesExp += r.SeriesExpGained;
            totals.Seconds += r.DurationSeconds;
        }
        Lifetime = totals;
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
            Svc.Log.Warning(ex, $"{ApsgConstants.LogPrefix} RunHistory save failed");
        }
    }

    public struct LifetimeTotals
    {
        public int Runs;
        public int Matches;
        public long SeriesExp;
        public double Seconds;

        public readonly double MatchesPerHour => Seconds > 0 ? Matches / (Seconds / 3600.0) : 0;
        public readonly TimeSpan Duration => TimeSpan.FromSeconds(Seconds);
    }
}
