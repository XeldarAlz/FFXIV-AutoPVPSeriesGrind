using AutoPvpSeriesGrind.Core.Game;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class QueueRouteTests
{
    [Theory]
    [InlineData(false, 40, 41)]
    [InlineData(true, 41, 40)]
    public void ConflictQueueNeverAcceptsTheOtherBracket(bool ranked, byte expected, byte other)
    {
        var route = PvpQueueRoute.For(MatchType.CrystallineConflict, ranked);
        Assert.Equal(expected, route.RouletteId);
        Assert.Equal(0u, route.DutyId);
        Assert.True(route.Matches(expected));
        Assert.False(route.Matches(other));
        Assert.False(route.Matches(7));
        Assert.False(route.Matches(0, PvpDuty.HiddenGorgeDutyId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RankedPreferenceDoesNotAffectOtherModes(bool ranked)
    {
        var frontline = PvpQueueRoute.For(MatchType.Frontline, ranked);
        var wings = PvpQueueRoute.For(MatchType.RivalWings, ranked);
        Assert.True(frontline.Matches(7));
        Assert.False(frontline.Matches(41));
        Assert.True(wings.Matches(0, PvpDuty.HiddenGorgeDutyId));
        Assert.False(wings.Matches(41, PvpDuty.HiddenGorgeDutyId));
        Assert.False(wings.Matches(0, 1));
        Assert.False(wings.Matches(0));
    }

    [Fact]
    public void UnknownModesAndEmptyRoutesCannotMatch()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PvpQueueRoute.For((MatchType)99, false));
        Assert.False(default(PvpQueueRoute).Matches(0));
    }
}
