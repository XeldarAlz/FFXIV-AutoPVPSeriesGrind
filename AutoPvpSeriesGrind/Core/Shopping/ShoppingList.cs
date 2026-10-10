using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoPvpSeriesGrind.Core.Shopping;

internal enum ShopTrigger : byte
{
    Threshold,
    Always,
}

internal readonly record struct ShopOrder(ShopItem Item, int StopAtCount);

internal sealed record ShoppingPlan(ShopOrder[] MarkOrders, ShopOrder[] CrystalOrders)
{
    public ShopOrder[] OrdersFor(PvpCurrency currency)
        => currency == PvpCurrency.WolfMarks ? MarkOrders : CrystalOrders;
}

internal static class ShoppingList
{
    public const int MaxStopAtCount = 999;

    private static readonly List<ShopOrder> scratch = new(capacity: 16);

    public static bool IsOwned(in ShopItem item)
        => PvpWallet.HeldCount(item.ItemId) > 0
        || (Svc.Data.GetExcelSheet<Item>().TryGetRow(item.ItemId, out var row) && Svc.UnlockState.IsItemUnlocked(row));

    public static int RemainingNeed(in ShopItem item, int stopAtCount)
    {
        if (item.IsCollectible)
        {
            return IsOwned(item) ? 0 : 1;
        }

        var held = PvpWallet.HeldCount(item.ItemId);
        var room = item.HoldLimit - held;
        var wanted = stopAtCount <= 0 ? int.MaxValue : stopAtCount - held;
        return Math.Max(0, Math.Min(room, wanted));
    }

    public static int Spendable(PvpCurrency currency)
        => Math.Max(0, PvpWallet.Held(currency) - ReserveOf(currency));

    public static int Affordable(in ShopItem item, int need, int budget)
        => item.Cost <= 0 ? 0 : Math.Max(0, Math.Min(need, budget / item.Cost));

    public static bool TryLeftoverItem(out ShopItem item)
    {
        item = default;
        var itemId = Plugin.Cfg.LeftoverMarksItemId;
        return itemId != 0
            && ShopCatalog.TryFind(itemId, out item)
            && item.Currency == PvpCurrency.WolfMarks
            && !item.IsCollectible;
    }

    public static ShoppingPlan? PlanTrip(ShopTrigger trigger, IReadOnlySet<uint> skippedItemIds)
    {
        var marksDue = trigger == ShopTrigger.Always || PvpWallet.Held(PvpCurrency.WolfMarks) >= Plugin.Cfg.ShopMarksThreshold;
        var markOrders = marksDue ? PlanCurrency(PvpCurrency.WolfMarks, skippedItemIds) : [];
        var crystalOrders = PlanCurrency(PvpCurrency.TrophyCrystals, skippedItemIds);
        if (markOrders.Length == 0 && crystalOrders.Length == 0)
        {
            return null;
        }

        return new ShoppingPlan(markOrders, crystalOrders);
    }

    private static int ReserveOf(PvpCurrency currency)
        => currency == PvpCurrency.WolfMarks ? Plugin.Cfg.KeepWolfMarks : 0;

    private static ShopOrder[] PlanCurrency(PvpCurrency currency, IReadOnlySet<uint> skippedItemIds)
    {
        scratch.Clear();
        var budget = Spendable(currency);
        var entries = Plugin.Cfg.ShoppingList;
        for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            var entry = entries[entryIndex];
            if (skippedItemIds.Contains(entry.ItemId) || !ShopCatalog.TryFind(entry.ItemId, out var item) || item.Currency != currency)
            {
                continue;
            }

            budget -= TakeOrder(item, entry.StopAtCount, budget);
        }

        if (currency == PvpCurrency.WolfMarks && TryLeftoverItem(out var leftover) && !skippedItemIds.Contains(leftover.ItemId))
        {
            TakeOrder(leftover, 0, budget);
        }

        return scratch.Count == 0 ? [] : [.. scratch];
    }

    private static int TakeOrder(in ShopItem item, int stopAtCount, int budget)
    {
        var quantity = Affordable(item, RemainingNeed(item, stopAtCount), budget);
        if (quantity <= 0)
        {
            return 0;
        }

        scratch.Add(new ShopOrder(item, stopAtCount));
        return quantity * item.Cost;
    }
}
