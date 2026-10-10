using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Shopping;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries
{
    private readonly HashSet<uint> shopSkippedItemIds = [];
    private bool shoppingSuspended;

    private async Task<bool> TryShopBetweenMatches()
    {
        if (shoppingSuspended || !Plugin.Cfg.ShopBetweenMatches || DutyOps.HasDutyInFlight() || !IsFreeToAct())
        {
            return false;
        }

        var plan = ShoppingList.PlanTrip(ShopTrigger.Threshold, shopSkippedItemIds);
        if (plan is null)
        {
            return false;
        }

        LogDiagnostic("shopping trigger met -> heading to Wolves' Den Pier before the next queue");
        SetPhase(AutoPhase.Shopping);
        var trip = new ShopTrip(plan, shopSkippedItemIds);
        var outcome = await RunChild(trip, ShopTrip.WatchdogMs, "shop-trip", DutyOps.HasDutyInFlight);
        if (outcome == ChildOutcome.TimedOut || trip.Faulted)
        {
            shoppingSuspended = true;
            Warn("shopping trip did not finish -> no more shopping this run");
        }

        Status = string.Empty;
        SetPhase(AutoPhase.Queueing);
        return true;
    }
}
