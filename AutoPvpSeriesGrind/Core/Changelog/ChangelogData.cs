using AutoPvpSeriesGrind.Core.Localization;

namespace AutoPvpSeriesGrind.Core.Changelog;

internal static class ChangelogData
{
    public static readonly ChangelogEntry[] Entries =
    [
        new("2.13.1.0", "2026-10-24", L.Changelog.Release21310),
        new("2.13.0.0", "2026-10-24", L.Changelog.Release21300),
        new("2.12.0.0", "2026-10-21", L.Changelog.Release21200),
        new("2.11.2.0", "2026-10-21", L.Changelog.Release21120),
        new("2.11.1.0", "2026-10-18", L.Changelog.Release21110),
        new("2.11.0.0", "2026-10-17", L.Changelog.Release21100),
        new("2.10.0.0", "2026-10-17", L.Changelog.Release21000),
        new("2.9.0.0", "2026-10-10", L.Changelog.Release2900),
        new("2.8.1.0", "2026-09-29", L.Changelog.Release2810),
        new("2.8.0.0", "2026-09-28", L.Changelog.Release2800),
        new("2.7.1.0", "2026-09-27", L.Changelog.Release2710),
        new("2.7.0.0", "2026-09-25", L.Changelog.Release2700),
        new("2.6.0.0", "2026-09-24", L.Changelog.Release2600),
    ];

    public static string LatestVersion => Entries[0].Version;

    // Newest first, so every entry above the one the player last saw is new to them. A version that is no longer
    // listed counts as older than all of them.
    public static int UnseenCount(string lastSeenVersion)
    {
        for (var index = 0; index < Entries.Length; index++)
        {
            if (string.Equals(Entries[index].Version, lastSeenVersion, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return Entries.Length;
    }
}
