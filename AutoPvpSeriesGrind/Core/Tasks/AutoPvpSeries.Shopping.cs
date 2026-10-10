using AutoPvpSeriesGrind.Core.Shopping;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries
{
    private readonly HashSet<uint> shopSkippedItemIds = [];
    private bool shoppingSuspended;

    private async Task<bool> TryShopBetweenMatches()
    {
        if (shoppingSuspended || !Plugin.Cfg.ShopBetweenMatches)
        {
            return false;
        }

        var plan = ShoppingList.PlanTrip(ShopTrigger.Threshold, shopSkippedItemIds);
        if (plan is null)
        {
            return false;
        }

        LogDiagnostic("shopping trigger met -> heading to Wolves' Den Pier before the next queue");
        await GoShopping(plan);
        return true;
    }

    private async Task ShopBeforeRunEnds()
    {
        if (shoppingSuspended || !Plugin.Cfg.ShopAtRunEnd)
        {
            return;
        }

        var plan = ShoppingList.PlanTrip(ShopTrigger.Always, shopSkippedItemIds);
        if (plan is null)
        {
            return;
        }

        LogDiagnostic("run ending -> spending PvP currency first");
        await GoShopping(plan);
    }

    private async Task GoShopping(ShoppingPlan plan)
    {
        SetPhase(AutoPhase.Shopping);
        var trip = new ShopTrip(plan, shopSkippedItemIds);
        var finished = await RunChild(trip, ShopTrip.WatchdogMs, "shop-trip");
        if (!finished || trip.Faulted)
        {
            shoppingSuspended = true;
            Warn("shopping trip did not finish -> no more shopping this run");
        }

        SetPhase(AutoPhase.Queueing);
    }
}
