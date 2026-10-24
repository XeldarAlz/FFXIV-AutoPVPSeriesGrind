namespace AutoPvpSeriesGrind.Core.Tasks;

internal enum StartBlocker : byte
{
    None,
    InvalidMode,
    LoggedOut,
    OtherDuty,
    QueueInFlight,
}

internal readonly record struct StartConditions(bool ModeValid, bool LoggedIn, bool InDuty, bool InSelectedArena, bool DutyInFlight);

internal static class StartPreflight
{
    public static StartBlocker Evaluate(in StartConditions conditions)
    {
        if (!conditions.ModeValid)
        {
            return StartBlocker.InvalidMode;
        }
        if (!conditions.LoggedIn)
        {
            return StartBlocker.LoggedOut;
        }
        if (conditions.InDuty)
        {
            return conditions.InSelectedArena ? StartBlocker.None : StartBlocker.OtherDuty;
        }

        return conditions.DutyInFlight ? StartBlocker.QueueInFlight : StartBlocker.None;
    }
}
