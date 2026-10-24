using AutoPvpSeriesGrind.Core.Localization;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal static class StartBlockerText
{
    public static LocString Title(StartBlocker blocker) => blocker switch
    {
        StartBlocker.InvalidMode => L.Grind.BlockedModeTitle,
        StartBlocker.LoggedOut => L.Grind.BlockedLoggedOutTitle,
        StartBlocker.OtherDuty => L.Grind.BlockedOtherDutyTitle,
        _ => L.Grind.BlockedQueueTitle,
    };

    public static LocString Detail(StartBlocker blocker) => blocker switch
    {
        StartBlocker.InvalidMode => L.Grind.BlockedModeDetail,
        StartBlocker.LoggedOut => L.Grind.BlockedLoggedOutDetail,
        StartBlocker.OtherDuty => L.Grind.BlockedOtherDutyDetail,
        _ => L.Grind.BlockedQueueDetail,
    };
}
