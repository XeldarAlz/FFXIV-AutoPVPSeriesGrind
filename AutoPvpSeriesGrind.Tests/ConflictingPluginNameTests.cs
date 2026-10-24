using AutoPvpSeriesGrind.Core.External;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class ConflictingPluginNameTests
{
    [Theory]
    [InlineData("RotationSolver")]
    [InlineData("Rotation Solver Reborn")]
    [InlineData("rotation-solver")]
    [InlineData("ROTATION_SOLVER_REBORN")]
    [InlineData("RotationSolverReborn (fork)")]
    public void MatchesTheNameAndItsRenamedForks(string name)
    {
        Assert.True(ConflictingPluginName.IsRotationSolver(name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Rotation")]
    [InlineData("AutoPvpSeriesGrind")]
    [InlineData("vnavmesh")]
    [InlineData("My Rotation Solver")]
    public void LeavesOtherPluginsAlone(string? name)
    {
        Assert.False(ConflictingPluginName.IsRotationSolver(name));
    }
}
