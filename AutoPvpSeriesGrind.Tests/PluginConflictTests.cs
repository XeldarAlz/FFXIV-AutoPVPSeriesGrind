using AutoPvpSeriesGrind.Core.External;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class PluginConflictTests
{
    private const string SourceUrl = "https://github.com/FFXIV-CombatReborn/RotationSolverReborn";

    [Theory]
    [InlineData("RotationSolver", "Renamed", null)]
    [InlineData("Renamed", "Rotation Solver Reborn", null)]
    [InlineData("Renamed", "Renamed", SourceUrl)]
    public void DetectsLoadedPluginsByEitherNameOrSource(string internalName, string name, string? source)
    {
        var check = new PluginConflictCheck(() => [new(internalName, name, source, true)], exception => Assert.Fail(exception.ToString()));
        var state = check.Read();
        Assert.True(state.Blocked);
        Assert.True(state.PluginLoaded);
        Assert.Equal(internalName, Assert.Single(state.LoadedPluginInternalNames));
    }

    [Fact]
    public void DisabledPluginDoesNotBlockEvenWithItsSourceUrlPresent()
    {
        var check = new PluginConflictCheck(() => [new("RotationSolver", "RSR", SourceUrl, false)],
            exception => Assert.Fail(exception.ToString()));
        Assert.False(check.Read().Blocked);
    }

    [Fact]
    public void NoInstalledPluginsDoesNotBlock()
    {
        var check = new PluginConflictCheck(() => [], exception => Assert.Fail(exception.ToString()));
        var state = check.Read();
        Assert.False(state.Blocked);
        Assert.False(state.PluginLoaded);
    }

    [Fact]
    public void UnrelatedPluginFromTheSameOrganizationIsNotMistakenForRsr()
    {
        var check = new PluginConflictCheck(() => [new("BossModReborn", "BossMod Reborn",
            "https://github.com/FFXIV-CombatReborn/BossmodReborn", true)], exception => Assert.Fail(exception.ToString()));
        Assert.False(check.Read().Blocked);
    }

    [Fact]
    public void ReadsHotLoadUnloadAndReenableImmediately()
    {
        var loaded = false;
        var check = new PluginConflictCheck(() => [new("RotationSolver", "RSR", SourceUrl, loaded)],
            exception => Assert.Fail(exception.ToString()));

        Assert.False(check.Read().Blocked);
        loaded = true;
        Assert.True(check.Read().Blocked);
        loaded = false;
        Assert.False(check.Read().Blocked);
        loaded = true;
        Assert.True(check.Read().Blocked);
    }

    [Fact]
    public void FailedStateReadsBlockAndRecoverWithoutCachingSuccess()
    {
        var fail = false;
        var reports = 0;
        var check = new PluginConflictCheck(
            () => fail ? throw new InvalidOperationException() : [],
            _ => reports++);

        Assert.False(check.Read().Blocked);
        fail = true;
        Assert.True(check.Read().Blocked);
        Assert.True(check.Read().DetectionFailed);
        Assert.Equal(1, reports);
        fail = false;
        Assert.False(check.Read().Blocked);
        fail = true;
        Assert.True(check.Read().Blocked);
        Assert.Equal(2, reports);
    }

    [Fact]
    public void PartialEnumerationFailurePreservesTheKnownConflict()
    {
        var check = new PluginConflictCheck(PartialPlugins, _ => { });
        var state = check.Read();
        Assert.True(state.Blocked);
        Assert.True(state.PluginLoaded);
        Assert.True(state.DetectionFailed);

        static IEnumerable<ConflictPlugin> PartialPlugins()
        {
            yield return new("RotationSolver", "RSR", SourceUrl, true);
            throw new InvalidOperationException();
        }
    }

    [Theory]
    [InlineData(SourceUrl)]
    [InlineData(" HTTPS://GITHUB.COM/ffxiv-combatreborn/rotationsolverreborn/ ")]
    [InlineData("https://github.com./FFXIV-CombatReborn/RotationSolverReborn")]
    [InlineData("https://cdn.jsdelivr.net/gh/FFXIV-CombatReborn/RotationSolverReborn@main/repo.json")]
    [InlineData("https://raw.githubusercontent.com/FFXIV-CombatReborn/RotationSolverReborn/main/repo.json")]
    public void RecognizesPluginSourceUrlVariants(string url)
        => Assert.True(ConflictingPluginSource.IsRotationSolverSource(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a URL")]
    [InlineData("https://example.com/FFXIV-CombatReborn/RotationSolverReborn")]
    [InlineData("https://github.com.example.com/FFXIV-CombatReborn/RotationSolverReborn")]
    [InlineData("https://github.com/OtherOwner/RotationSolverReborn")]
    [InlineData("https://github.com/FFXIV-CombatReborn/RotationSolverRebornBackup")]
    [InlineData("https://example.com/?url=https://github.com/FFXIV-CombatReborn/RotationSolverReborn")]
    [InlineData("https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json")]
    [InlineData("file:///FFXIV-CombatReborn/RotationSolverReborn")]
    public void DoesNotTreatSharedRepositoriesOrLookalikeUrlsAsThePluginSource(string? url)
        => Assert.False(ConflictingPluginSource.IsRotationSolverSource(url));

    [Theory]
    [InlineData("RotationSolver")]
    [InlineData("RotationSolver-Reborn_v2.0")]
    public void BuildsOneDisableCommandForAnInternalIdentifier(string internalName)
        => Assert.Equal($"/xldisableplugin \"{internalName}\"", PluginDisableCommand.Create(internalName));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("RotationSolver\" /xlkill")]
    [InlineData("RotationSolver\n/xlkill")]
    [InlineData("RotationSolver\r/xlkill")]
    [InlineData("RotationSolver\0")]
    [InlineData("Rotation Solver Reborn")]
    public void RejectsUnsafeCommandIdentifiers(string? internalName)
        => Assert.Null(PluginDisableCommand.Create(internalName));
}
