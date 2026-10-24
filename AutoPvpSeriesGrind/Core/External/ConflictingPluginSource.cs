namespace AutoPvpSeriesGrind.Core.External;

internal static class ConflictingPluginSource
{
    public static bool IsRotationSolverSource(string? url)
        => IsGitHubRepository(url, "RotationSolverReborn");

    private static bool IsGitHubRepository(string? url, string repository)
    {
        if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)) return false;

        var parts = Uri.UnescapeDataString(uri.AbsolutePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
        var offset = 0;
        var host = uri.Host.TrimEnd('.');
        if (host.Equals("cdn.jsdelivr.net", StringComparison.OrdinalIgnoreCase))
        {
            if (parts.Length < 3 || !parts[0].Equals("gh", StringComparison.OrdinalIgnoreCase)) return false;
            offset = 1;
        }
        else if (!host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("raw.github.com", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("github.com", StringComparison.OrdinalIgnoreCase)) return false;

        if (parts.Length < offset + 2) return false;
        var repo = parts[offset + 1].Split('@')[0];
        return parts[offset].Equals("FFXIV-CombatReborn", StringComparison.OrdinalIgnoreCase)
            && repo.Equals(repository, StringComparison.OrdinalIgnoreCase);
    }
}
