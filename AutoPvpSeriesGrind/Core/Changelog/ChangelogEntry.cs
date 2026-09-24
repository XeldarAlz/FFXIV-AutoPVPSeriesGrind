using AutoPvpSeriesGrind.Core.Localization;

namespace AutoPvpSeriesGrind.Core.Changelog;

internal readonly record struct ChangelogEntry(string Version, string Date, LocString[] Highlights);
