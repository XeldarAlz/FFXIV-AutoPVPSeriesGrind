using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Shopping;
using clib.TaskSystem;
using ECommons.DalamudServices;
using System.Text;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class ShopTrip(ShoppingPlan plan, HashSet<uint> skippedItemIds) : AutoCommon
{
    public const int WatchdogMs = 600_000;

    private const int ReadyWaitMs = 20_000;
    private const int InteractWaitMs = 12_000;
    private const int InteractRetryMs = 3_000;
    private const int ShopOpenWaitMs = 8_000;
    private const int ListWaitMs = 2_000;
    private const int MenuReturnWaitMs = 2_000;
    private const int BuyClickWaitMs = 2_000;
    private const int SpendWaitMs = 8_000;
    private const int ConfirmCloseWaitMs = 2_000;
    private const int CategoryWaitMs = 4_000;
    private const int CategoryRetryMs = 1_000;
    private const int CategorySettleMs = 500;
    private const int SettleMs = 400;
    private const int PollMs = 100;
    private const int MaxPerPurchase = 99;

    private readonly ShoppingPlan plan = plan;
    private readonly HashSet<uint> skippedItemIds = skippedItemIds;
    private readonly List<(ShopItem Item, int Count)> receipt = [];
    private int selectedInclusionCategory = -1;

    public bool Faulted { get; private set; }

    protected override async Task Execute()
    {
        try
        {
            await Shop();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Faulted = true;
            Warn($"Shopping trip failed: {exception}");
            ApsgChat.PrintError(Loc.T(L.Shop.ChatFailed, exception.Message));
        }
        finally
        {
            ShopWindows.CloseAll();
        }
    }

    private async Task Shop()
    {
        if (!await WaitUntilTimed(CanTravel, ReadyWaitMs, "shop-ready"))
        {
            Warn("Shopping: still in combat, a duty or the queue; skipping the trip.");
            ApsgChat.PrintError(Loc.T(L.Shop.ChatBusy));
            return;
        }

        ApsgChat.Print(Loc.T(L.Shop.ChatStart, DescribePlan()));

        await VisitVendor(PvpVendors.MarkQuartermaster);
        await VisitVendor(PvpVendors.CrystalQuartermaster);

        if (receipt.Count == 0)
        {
            ApsgChat.Print(Loc.T(L.Shop.ChatNothingBought));
            return;
        }

        ApsgChat.Print(Loc.T(L.Shop.ChatDone, DescribeReceipt()));
    }

    private async Task VisitVendor(PvpVendor vendor)
    {
        var orders = plan.OrdersFor(vendor.Currency);
        if (orders.Length == 0)
        {
            return;
        }

        await ReachVendor(vendor);
        for (var orderIndex = 0; orderIndex < orders.Length; orderIndex++)
        {
            await BuyOrder(vendor, orders[orderIndex]);
        }

        ShopWindows.CloseAll();
        await DelayMs(SettleMs);
    }

    private async Task ReachVendor(PvpVendor vendor)
    {
        if (Svc.ClientState.TerritoryType != PvpVendors.WolvesDenPierTerritoryId)
        {
            Status = "Teleporting to Wolves' Den Pier";
            LogDiagnostic("Shopping: teleporting to Wolves' Den Pier.");
            await TeleportTo(PvpVendors.WolvesDenPierTerritoryId, vendor.Position);
        }

        Status = "Walking to the vendor";
        if (!await TryMoveTo(vendor.Position, MovementConfig.InteractRange, allowTeleportIfFaster: false, allowAethernet: false))
        {
            LogDiagnostic($"Shopping: no path to vendor {vendor.EnpcBaseId}; trying to talk from here.");
        }
    }

    private async Task BuyOrder(PvpVendor vendor, ShopOrder order)
    {
        var item = order.Item;
        while (true)
        {
            var need = ShoppingList.RemainingNeed(item, order.StopAtCount);
            var quantity = Math.Min(ShoppingList.Affordable(item, need, ShoppingList.Spendable(item.Currency)),
                item.IsStackable ? MaxPerPurchase : 1);
            if (quantity <= 0)
            {
                return;
            }

            if (!await ShowItem(vendor, item))
            {
                SkipItem(item, "is not listed in the vendor's shop (it may be filtered for your current job)");
                return;
            }

            if (!await Purchase(item, quantity))
            {
                SkipItem(item, "did not go through (full inventory, or the purchase was refused)");
                return;
            }
        }
    }

    private async Task<bool> Purchase(ShopItem item, int quantity)
    {
        var before = PvpWallet.Held(item.Currency);
        Status = $"Buying {quantity} x {item.Name}";
        LogDiagnostic($"Shopping: buying {quantity} x {item.Name} ({item.Cost} each, wallet {before}).");
        if (!await WaitUntilTimed(() => ShopWindows.Buy(item, quantity), BuyClickWaitMs, "shop-buy"))
        {
            return false;
        }

        var spent = await WaitUntilTimed(() =>
        {
            ShopWindows.Confirm();
            return PvpWallet.Held(item.Currency) < before;
        }, SpendWaitMs, "shop-spend");
        if (!spent)
        {
            return false;
        }

        Record(item, (before - PvpWallet.Held(item.Currency)) / item.Cost);
        await WaitUntilTimed(() => !ShopWindows.ConfirmOpen(), ConfirmCloseWaitMs, "shop-confirm-close");
        await DelayMs(SettleMs);
        return true;
    }

    private Task<bool> ShowItem(PvpVendor vendor, ShopItem item)
        => vendor.Currency == PvpCurrency.WolfMarks ? ShowInInclusionShop(vendor, item) : ShowInCurrencyShop(vendor, item);

    private async Task<bool> ShowInInclusionShop(PvpVendor vendor, ShopItem item)
    {
        if (ShopWindows.InclusionShopOpen() && ShopWindows.InclusionShopLists(item.ItemId))
        {
            return true;
        }

        if (!ShopWindows.InclusionShopOpen())
        {
            selectedInclusionCategory = -1;
            if (!await TalkTo(vendor, ShopWindows.InclusionShopOpen))
            {
                return false;
            }

            LogDiagnostic($"Shopping: inclusion shop open; {ShopWindows.DescribeInclusionShop()}");
        }

        var candidates = InclusionTabCandidates(item);
        for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            var (category, tab) = candidates[candidateIndex];
            if (!await TryInclusionTab(item, category, tab))
            {
                continue;
            }

            if (candidateIndex > 0)
            {
                LogDiagnostic($"Shopping: {item.Name} found in category {category}, tab {tab} (expected {item.Menu}/{item.SubMenu + 1}).");
            }

            return true;
        }

        return false;
    }

    private static List<(int Category, int Tab)> InclusionTabCandidates(in ShopItem item)
    {
        var subMenuCounts = ShopCatalog.SubMenuCountsOf(PvpCurrency.WolfMarks);
        var candidates = new List<(int Category, int Tab)>();
        AddCandidate(candidates, subMenuCounts, item.Menu, item.SubMenu + 1);
        AddCandidate(candidates, subMenuCounts, item.Menu, item.SubMenu);
        for (var category = 0; category < subMenuCounts.Length; category++)
        {
            AddCandidate(candidates, subMenuCounts, category, item.SubMenu + 1);
            AddCandidate(candidates, subMenuCounts, category, item.SubMenu);
        }

        for (var category = 0; category < subMenuCounts.Length; category++)
        {
            for (var tab = 0; tab <= subMenuCounts[category]; tab++)
            {
                AddCandidate(candidates, subMenuCounts, category, tab);
            }
        }

        return candidates;
    }

    private static void AddCandidate(List<(int Category, int Tab)> candidates, ReadOnlySpan<byte> subMenuCounts, int category, int tab)
    {
        if (category >= subMenuCounts.Length || tab > subMenuCounts[category] || candidates.Contains((category, tab)))
        {
            return;
        }

        candidates.Add((category, tab));
    }

    private async Task<bool> TryInclusionTab(ShopItem item, int category, int tab)
    {
        Status = $"Looking for {item.Name}";
        if (!await ShowInclusionCategory(category))
        {
            LogDiagnostic($"Shopping: category {category} did not open; {ShopWindows.DescribeInclusionShop()}");
            return false;
        }

        if (!ShopWindows.SelectInclusionSubCategory(tab))
        {
            return false;
        }

        if (await WaitUntilTimed(() => ShopWindows.InclusionShopLists(item.ItemId), ListWaitMs, $"shop-tab-{category}-{tab}"))
        {
            return true;
        }

        LogDiagnostic($"Shopping: no {item.Name} in category {category}, tab {tab}; {ShopWindows.DescribeInclusionShop()}");
        return false;
    }

    // The shop can drop a category pick made while it is still loading, so keep asking until it reports that category.
    private async Task<bool> ShowInclusionCategory(int category)
    {
        if (category == selectedInclusionCategory && ShopWindows.InclusionCategoryReady(category))
        {
            return true;
        }

        selectedInclusionCategory = -1;
        var requested = false;
        var nextSelectAtMs = 0L;
        var shown = await WaitUntilTimed(() =>
        {
            if (requested && ShopWindows.InclusionCategoryReady(category))
            {
                return true;
            }

            var now = Environment.TickCount64;
            if (now >= nextSelectAtMs && ShopWindows.SelectInclusionCategory(category))
            {
                requested = true;
                nextSelectAtMs = now + CategoryRetryMs;
            }

            return false;
        }, CategoryWaitMs, $"shop-category-{category}");
        if (!shown)
        {
            return false;
        }

        selectedInclusionCategory = category;
        await DelayMs(CategorySettleMs);
        return true;
    }

    private static bool CanTravel() => IsFreeToAct() && !DutyOps.HasDutyInFlight();

    private async Task<bool> ShowInCurrencyShop(PvpVendor vendor, ShopItem item)
    {
        if (ShopWindows.CurrencyShopOpen())
        {
            if (ShopWindows.CurrencyShopLists(item.ItemId))
            {
                return true;
            }

            ShopWindows.CloseCurrencyShop();
            await WaitUntilTimed(ShopWindows.MenuOpen, MenuReturnWaitMs, "shop-menu-return");
        }

        if (await TryMenuEntry(vendor, item, item.Menu))
        {
            return true;
        }

        var menuCount = ShopCatalog.SubMenuCountsOf(PvpCurrency.TrophyCrystals).Length;
        for (var entry = 0; entry < menuCount; entry++)
        {
            if (entry == item.Menu || !await TryMenuEntry(vendor, item, entry))
            {
                continue;
            }

            LogDiagnostic($"Shopping: {item.Name} found in menu entry {entry} (expected {item.Menu}).");
            return true;
        }

        return false;
    }

    private async Task<bool> TryMenuEntry(PvpVendor vendor, ShopItem item, int entry)
    {
        if (!ShopWindows.MenuOpen() && !await TalkTo(vendor, () => ShopWindows.MenuOpen() || ShopWindows.CurrencyShopOpen()))
        {
            return false;
        }

        if (!ShopWindows.CurrencyShopOpen())
        {
            Status = $"Looking for {item.Name}";
            if (entry >= ShopWindows.MenuEntryCount() || !await WaitUntilTimed(() => ShopWindows.PickMenuEntry(entry), MenuReturnWaitMs, $"shop-menu-{entry}"))
            {
                return false;
            }

            if (!await WaitUntilTimed(ShopWindows.CurrencyShopOpen, ShopOpenWaitMs, $"shop-open-{entry}"))
            {
                return false;
            }
        }

        if (await WaitUntilTimed(() => ShopWindows.CurrencyShopLists(item.ItemId), ListWaitMs, $"shop-list-{entry}"))
        {
            return true;
        }

        ShopWindows.CloseCurrencyShop();
        await WaitUntilTimed(ShopWindows.MenuOpen, MenuReturnWaitMs, $"shop-menu-return-{entry}");
        return false;
    }

    private async Task<bool> TalkTo(PvpVendor vendor, Func<bool> opened)
    {
        Status = "Talking to the vendor";
        var deadline = Environment.TickCount64 + InteractWaitMs;
        var nextInteractAt = 0L;
        while (Environment.TickCount64 < deadline)
        {
            if (opened())
            {
                return true;
            }

            ShopWindows.AdvanceTalk();
            if (Environment.TickCount64 >= nextInteractAt)
            {
                if (!ShopWindows.Interact(vendor.EnpcBaseId))
                {
                    LogDiagnostic($"Shopping: vendor {vendor.EnpcBaseId} is not loaded nearby.");
                }

                nextInteractAt = Environment.TickCount64 + InteractRetryMs;
            }

            await DelayMs(PollMs);
        }

        LogDiagnostic($"Shopping: vendor {vendor.EnpcBaseId} did not open the expected window within {InteractWaitMs / 1000}s " +
            $"(menu {ShopWindows.MenuOpen()}, inclusion shop {ShopWindows.InclusionShopOpen()}, currency shop {ShopWindows.CurrencyShopOpen()}).");
        return false;
    }

    private void SkipItem(ShopItem item, string reason)
    {
        skippedItemIds.Add(item.ItemId);
        Warn($"Shopping: {item.Name} {reason}; skipping it for the rest of the run.");
    }

    private void Record(ShopItem item, int count)
    {
        for (var entryIndex = 0; entryIndex < receipt.Count; entryIndex++)
        {
            if (receipt[entryIndex].Item.ItemId != item.ItemId)
            {
                continue;
            }

            receipt[entryIndex] = (item, receipt[entryIndex].Count + count);
            return;
        }

        receipt.Add((item, count));
    }

    private string DescribePlan()
    {
        var builder = new StringBuilder();
        var named = new HashSet<uint>();
        AppendNames(builder, named, plan.MarkOrders);
        AppendNames(builder, named, plan.CrystalOrders);
        return builder.ToString();
    }

    private static void AppendNames(StringBuilder builder, HashSet<uint> named, ShopOrder[] orders)
    {
        for (var orderIndex = 0; orderIndex < orders.Length; orderIndex++)
        {
            var item = orders[orderIndex].Item;
            if (!named.Add(item.ItemId))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(item.Name);
        }
    }

    private string DescribeReceipt()
    {
        var builder = new StringBuilder();
        for (var entryIndex = 0; entryIndex < receipt.Count; entryIndex++)
        {
            if (entryIndex > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Loc.T(L.Shop.QuantityItem, receipt[entryIndex].Count, receipt[entryIndex].Item.Name));
        }

        return builder.ToString();
    }
}
