using AutoPvpSeriesGrind.Core.Tasks;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class StartPreflightTests
{
    private static readonly StartConditions Settled = new(ModeValid: true, LoggedIn: true, InDuty: false, InSelectedArena: false, DutyInFlight: false);

    [Fact]
    public void SettledCharacterOutsideDutyCanStart()
    {
        Assert.Equal(StartBlocker.None, StartPreflight.Evaluate(Settled));
    }

    [Fact]
    public void StartingInsideTheSelectedArenaIsAllowed()
    {
        var conditions = Settled with { InDuty = true, InSelectedArena = true };
        Assert.Equal(StartBlocker.None, StartPreflight.Evaluate(conditions));
    }

    [Fact]
    public void AnyOtherDutyBlocksTheStart()
    {
        var conditions = Settled with { InDuty = true, InSelectedArena = false };
        Assert.Equal(StartBlocker.OtherDuty, StartPreflight.Evaluate(conditions));
    }

    [Fact]
    public void QueueInFlightBlocksTheStart()
    {
        Assert.Equal(StartBlocker.QueueInFlight, StartPreflight.Evaluate(Settled with { DutyInFlight = true }));
    }

    [Fact]
    public void LoggedOutWinsOverQueueState()
    {
        var conditions = Settled with { LoggedIn = false, DutyInFlight = true };
        Assert.Equal(StartBlocker.LoggedOut, StartPreflight.Evaluate(conditions));
    }

    [Fact]
    public void InvalidModeIsReportedFirst()
    {
        var conditions = Settled with { ModeValid = false, LoggedIn = false };
        Assert.Equal(StartBlocker.InvalidMode, StartPreflight.Evaluate(conditions));
    }
}
