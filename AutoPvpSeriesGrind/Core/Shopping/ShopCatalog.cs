using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoPvpSeriesGrind.Core.Shopping;

[Flags]
internal enum ShopItemTraits : byte
{
    None = 0,
    Collectible = 1,
    Stackable = 2,
    Equipment = 4,
}

internal readonly record struct ShopItem(
    uint ItemId,
    string Name,
    PvpCurrency Currency,
    int Cost,
    byte Menu,
    byte SubMenu,
    ShopItemTraits Traits,
    int HoldLimit)
{
    public bool IsCollectible => (Traits & ShopItemTraits.Collectible) != 0;
    public bool IsStackable => (Traits & ShopItemTraits.Stackable) != 0;
    public bool IsEquipment => (Traits & ShopItemTraits.Equipment) != 0;
}

internal static class ShopCatalog
{
    public const uint VentureItemId = 21072;

    private const uint CurrencyUiCategoryId = 100;

    private static ShopItem[]? items;
    private static byte[] markSubMenuCounts = [];
    private static byte[] crystalSubMenuCounts = [];

    public static ShopItem[] All => items ??= Build();

    public static bool TryFind(uint itemId, out ShopItem item)
    {
        var catalog = All;
        for (var itemIndex = 0; itemIndex < catalog.Length; itemIndex++)
        {
            if (catalog[itemIndex].ItemId != itemId)
            {
                continue;
            }

            item = catalog[itemIndex];
            return true;
        }

        item = default;
        return false;
    }

    public static ReadOnlySpan<byte> SubMenuCountsOf(PvpCurrency currency)
    {
        _ = All;
        return currency == PvpCurrency.WolfMarks ? markSubMenuCounts : crystalSubMenuCounts;
    }

    private static ShopItem[] Build()
    {
        var found = new List<ShopItem>(capacity: 512);
        var seen = new HashSet<uint>();
        markSubMenuCounts = CollectInclusionShop(PvpVendors.MarkQuartermaster, found, seen);
        var markCount = found.Count;
        crystalSubMenuCounts = CollectSpecialShops(PvpVendors.CrystalQuartermaster, found, seen);
        found.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        RunLog.Info($"Shop catalog: {markCount} Wolf Mark items in {markSubMenuCounts.Length} categories, {found.Count - markCount} Trophy Crystal items in {crystalSubMenuCounts.Length} shops.");
        return [.. found];
    }

    private static byte[] CollectInclusionShop(PvpVendor vendor, List<ShopItem> found, HashSet<uint> seen)
    {
        var inclusionShops = Svc.Data.GetExcelSheet<InclusionShop>();
        var categories = Svc.Data.GetExcelSheet<InclusionShopCategory>();
        var series = Svc.Data.GetSubrowExcelSheet<InclusionShopSeries>();
        if (!Svc.Data.GetExcelSheet<ENpcBase>().TryGetRow(vendor.EnpcBaseId, out var npc))
        {
            return [];
        }

        for (var dataIndex = 0; dataIndex < npc.ENpcData.Count; dataIndex++)
        {
            if (!inclusionShops.TryGetRow(npc.ENpcData[dataIndex].RowId, out var shop))
            {
                continue;
            }

            var subMenuCounts = new List<byte>(shop.Category.Count);
            for (var categoryIndex = 0; categoryIndex < shop.Category.Count; categoryIndex++)
            {
                var categoryId = shop.Category[categoryIndex].RowId;
                if (categoryId == 0 || !categories.TryGetRow(categoryId, out var category))
                {
                    continue;
                }

                var menu = (byte)subMenuCounts.Count;
                if (!series.TryGetRow(category.InclusionShopSeries.RowId, out var subShops))
                {
                    subMenuCounts.Add(0);
                    continue;
                }

                for (var subIndex = 0; subIndex < subShops.Count; subIndex++)
                {
                    CollectSpecialShop(subShops[subIndex].SpecialShop.RowId, vendor.Currency, menu, (byte)subIndex, found, seen);
                }

                subMenuCounts.Add((byte)subShops.Count);
            }

            return [.. subMenuCounts];
        }

        RunLog.Warning($"Vendor {vendor.EnpcBaseId} has no inclusion shop in game data.");
        return [];
    }

    private static byte[] CollectSpecialShops(PvpVendor vendor, List<ShopItem> found, HashSet<uint> seen)
    {
        var specialShops = Svc.Data.GetExcelSheet<SpecialShop>();
        if (!Svc.Data.GetExcelSheet<ENpcBase>().TryGetRow(vendor.EnpcBaseId, out var npc))
        {
            return [];
        }

        var menuCount = 0;
        for (var dataIndex = 0; dataIndex < npc.ENpcData.Count; dataIndex++)
        {
            var shopId = npc.ENpcData[dataIndex].RowId;
            if (!specialShops.HasRow(shopId))
            {
                continue;
            }

            CollectSpecialShop(shopId, vendor.Currency, (byte)menuCount, 0, found, seen);
            menuCount++;
        }

        if (menuCount == 0)
        {
            RunLog.Warning($"Vendor {vendor.EnpcBaseId} has no special shops in game data.");
        }

        return new byte[menuCount];
    }

    private static void CollectSpecialShop(uint shopId, PvpCurrency currency, byte menu, byte subMenu,
        List<ShopItem> found, HashSet<uint> seen)
    {
        var itemSheet = Svc.Data.GetExcelSheet<Item>();
        if (!Svc.Data.GetExcelSheet<SpecialShop>().TryGetRow(shopId, out var shop))
        {
            return;
        }

        var currencyItemId = PvpWallet.ItemIdOf(currency);
        for (var entryIndex = 0; entryIndex < shop.Item.Count; entryIndex++)
        {
            var entry = shop.Item[entryIndex];
            if (entry.ReceiveItems.Count == 0 || entry.ItemCosts.Count == 0)
            {
                continue;
            }

            var price = entry.ItemCosts[0];
            var needsSecondCost = entry.ItemCosts.Count > 1 && entry.ItemCosts[1].ItemCost.RowId != 0;
            if (price.ItemCost.RowId != currencyItemId || price.CurrencyCost == 0 || needsSecondCost)
            {
                continue;
            }

            var itemId = entry.ReceiveItems[0].Item.RowId;
            if (itemId == 0 || !itemSheet.TryGetRow(itemId, out var row) || !seen.Add(itemId))
            {
                continue;
            }

            var name = row.Name.ExtractText();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            found.Add(new ShopItem(itemId, name, currency, (int)price.CurrencyCost, menu, subMenu, TraitsOf(row), HoldLimitOf(row)));
        }
    }

    private static ShopItemTraits TraitsOf(Item row)
    {
        var traits = ShopItemTraits.None;
        if (row.IsUnique || Svc.UnlockState.IsItemUnlockable(row))
        {
            traits |= ShopItemTraits.Collectible;
        }

        if (row.StackSize > 1)
        {
            traits |= ShopItemTraits.Stackable;
        }

        if (row.EquipSlotCategory.RowId != 0)
        {
            traits |= ShopItemTraits.Equipment;
        }

        return traits;
    }

    private static int HoldLimitOf(Item row)
        => row.ItemUICategory.RowId == CurrencyUiCategoryId ? (int)row.StackSize : int.MaxValue;
}
