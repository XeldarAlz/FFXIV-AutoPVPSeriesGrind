using AutoPvpSeriesGrind.Core.Localization;

namespace AutoPvpSeriesGrind.Core.External;

internal sealed record ExternalPluginInfo(
    string InternalName,
    string DisplayName,
    string RepoUrl,
    LocString Purpose,
    bool Required,
    string[]? Aliases = null);
