namespace AutoPvpSeriesGrind.Core.External;

internal readonly record struct ConflictPlugin(string InternalName, string Name, string? SourceUrl, bool IsLoaded);
internal sealed record PluginConflictState(IReadOnlyList<string> LoadedPluginInternalNames, bool DetectionFailed)
{
    public bool PluginLoaded => LoadedPluginInternalNames.Count > 0;
    public bool Blocked => PluginLoaded || DetectionFailed;
}

// Read live state at each gate. Failed reads must not look like an empty, compatible installation.
internal sealed class PluginConflictCheck(
    Func<IEnumerable<ConflictPlugin>> readPlugins,
    Action<Exception> reportFailure)
{
    private bool failureReported;

    public PluginConflictState Read()
    {
        List<string> loaded = [];
        var failed = false;
        try
        {
            foreach (var plugin in readPlugins())
            {
                if (plugin.IsLoaded && (ConflictingPluginName.IsRotationSolver(plugin.InternalName)
                    || ConflictingPluginName.IsRotationSolver(plugin.Name)
                    || ConflictingPluginSource.IsRotationSolverSource(plugin.SourceUrl)))
                    loaded.Add(plugin.InternalName);
            }
        }
        catch (Exception exception)
        {
            failed = true;
            if (!failureReported) reportFailure(exception);
            failureReported = true;
        }

        failureReported = failed;
        return new PluginConflictState(loaded, failed);
    }
}
