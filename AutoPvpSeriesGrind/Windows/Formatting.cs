using AutoPvpSeriesGrind.Core.Localization;

namespace AutoPvpSeriesGrind.Windows;

internal static class Formatting
{
    public static string Exp(long exp)
    {
        if (exp >= 1_000_000) return $"{exp / 1_000_000.0:0.0}M";
        if (exp >= 1_000) return $"{exp / 1_000.0:0.0}K";
        return exp.ToString();
    }

    public static string Elapsed(TimeSpan t)
    {
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes:D2}m";
        return $"{t.Minutes}m {t.Seconds:D2}s";
    }

    public static string Time(float secs)
    {
        if (secs <= 0) return "--:--";
        var s = (int)secs;
        return $"{s / 60}:{s % 60:D2}";
    }

    public static string RelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalSeconds < 60) return Loc.T(L.History.JustNow);
        if (span.TotalMinutes < 60) return Loc.T(L.History.MinutesAgo, (int)span.TotalMinutes);
        if (span.TotalHours < 24) return Loc.T(L.History.HoursAgo, (int)span.TotalHours);
        if (span.TotalDays < 7) return Loc.T(L.History.DaysAgo, (int)span.TotalDays);
        return utc.ToLocalTime().ToString("MMM d", Loc.Culture);
    }
}
