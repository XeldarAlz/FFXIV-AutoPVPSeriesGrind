namespace AutoPvpSeriesGrind.Core.External;

internal sealed record ExternalPluginInfo(
    string InternalName,
    string DisplayName,
    string RepoUrl,
    string Purpose,
    bool Required,
    string[]? Aliases = null);
