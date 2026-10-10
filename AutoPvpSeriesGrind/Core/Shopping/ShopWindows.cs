using AutoPvpSeriesGrind.Core.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;

namespace AutoPvpSeriesGrind.Core.Shopping;

internal static unsafe class ShopWindows
{
    private const string InclusionShopAddon = "InclusionShop";
    private const string CurrencyShopAddon = "ShopExchangeCurrency";
    private const string IconMenuAddon = "SelectIconString";
    private const string MenuAddon = "SelectString";
    private const string TalkAddon = "Talk";
    private const string YesnoAddon = "SelectYesno";
    private const string ItemDialogAddon = "ShopExchangeItemDialog";
    private const string CurrencyDialogAddon = "ShopExchangeCurrencyDialog";

    private const int InclusionCategoryEvent = 12;
    private const int InclusionSubCategoryEvent = 13;

    private const int ClickThrottleMs = 250;
    private const string ClickThrottleKey = "AutoPvpSeriesGrind.Shop.Click";
    private const string ConfirmThrottleKey = "AutoPvpSeriesGrind.Shop.Confirm";
    private const string TalkThrottleKey = "AutoPvpSeriesGrind.Shop.Talk";

    private static readonly string[] closableAddons = [InclusionShopAddon, CurrencyShopAddon, IconMenuAddon, MenuAddon];

    public static bool InclusionShopOpen() => AddonProbe.IsReady(InclusionShopAddon);

    public static bool CurrencyShopOpen() => AddonProbe.IsReady(CurrencyShopAddon);

    public static bool MenuOpen() => AddonProbe.IsReady(IconMenuAddon) || AddonProbe.IsReady(MenuAddon);

    public static bool ConfirmOpen()
        => AddonProbe.IsReady(YesnoAddon) || AddonProbe.IsReady(ItemDialogAddon) || AddonProbe.IsReady(CurrencyDialogAddon);

    public static bool Interact(uint enpcBaseId)
    {
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject.ObjectKind != ObjectKind.EventNpc || gameObject.BaseId != enpcBaseId)
            {
                continue;
            }

            TargetSystem.Instance()->InteractWithObject((GameObjectStruct*)gameObject.Address, true);
            return true;
        }

        return false;
    }

    public static void AdvanceTalk()
    {
        if (!AddonProbe.TryGetReady(TalkAddon, out var addon) || !EzThrottler.Throttle(TalkThrottleKey, ClickThrottleMs))
        {
            return;
        }

        new AddonMaster.Talk((nint)addon).Click();
    }

    public static bool SelectInclusionCategory(int category) => FireInclusionEvent(InclusionCategoryEvent, category);

    public static bool SelectInclusionSubCategory(int subCategory) => FireInclusionEvent(InclusionSubCategoryEvent, subCategory);

    private static bool FireInclusionEvent(int eventId, int index)
    {
        if (!AddonProbe.TryGetReady(InclusionShopAddon, out var addon))
        {
            return false;
        }

        Callback.Fire(addon, true, eventId, index);
        return true;
    }

    public static bool InclusionShopLists(uint itemId) => FindInclusionItem(itemId) is not null;

    public static bool CurrencyShopLists(uint itemId) => FindCurrencyItem(itemId) is not null;

    public static bool Buy(in ShopItem item, int quantity)
    {
        if (!EzThrottler.Throttle(ClickThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        if (item.Currency == PvpCurrency.WolfMarks)
        {
            var inclusionEntry = FindInclusionItem(item.ItemId);
            inclusionEntry?.Select(quantity);
            return inclusionEntry is not null;
        }

        var currencyEntry = FindCurrencyItem(item.ItemId);
        currencyEntry?.Select(quantity);
        return currencyEntry is not null;
    }

    public static void Confirm()
    {
        if (!EzThrottler.Throttle(ConfirmThrottleKey, ClickThrottleMs))
        {
            return;
        }

        if (AddonProbe.TryGetReady(YesnoAddon, out var yesno))
        {
            new AddonMaster.SelectYesno((nint)yesno).Yes();
            return;
        }

        if (AddonProbe.TryGetReady(ItemDialogAddon, out var itemDialog))
        {
            new AddonMaster.ShopExchangeItemDialog((nint)itemDialog).Exchange();
            return;
        }

        if (AddonProbe.TryGetReady(CurrencyDialogAddon, out var currencyDialog))
        {
            new AddonMaster.ShopExchangeCurrencyDialog((nint)currencyDialog).Exchange();
        }
    }

    public static int MenuEntryCount()
    {
        if (AddonProbe.TryGetReady(IconMenuAddon, out var iconMenu))
        {
            return new AddonMaster.SelectIconString((nint)iconMenu).EntryCount;
        }

        return AddonProbe.TryGetReady(MenuAddon, out var menu) ? new AddonMaster.SelectString((nint)menu).EntryCount : 0;
    }

    public static bool PickMenuEntry(int index)
    {
        if (!EzThrottler.Throttle(ClickThrottleKey, ClickThrottleMs))
        {
            return false;
        }

        if (AddonProbe.TryGetReady(IconMenuAddon, out var iconMenu))
        {
            var iconEntries = new AddonMaster.SelectIconString((nint)iconMenu).Entries;
            if (index < 0 || index >= iconEntries.Length)
            {
                return false;
            }

            iconEntries[index].Select();
            return true;
        }

        if (!AddonProbe.TryGetReady(MenuAddon, out var menu))
        {
            return false;
        }

        var entries = new AddonMaster.SelectString((nint)menu).Entries;
        if (index < 0 || index >= entries.Length)
        {
            return false;
        }

        entries[index].Select();
        return true;
    }

    public static void CloseCurrencyShop()
    {
        if (AddonProbe.TryGetReady(CurrencyShopAddon, out var addon))
        {
            addon->Close(true);
        }
    }

    public static void CloseAll()
    {
        for (var addonIndex = 0; addonIndex < closableAddons.Length; addonIndex++)
        {
            if (AddonProbe.TryGetReady(closableAddons[addonIndex], out var addon))
            {
                addon->Close(true);
            }
        }
    }

    private static AddonMaster.InclusionShop.ShopItemInfo? FindInclusionItem(uint itemId)
    {
        if (!AddonProbe.TryGetReady(InclusionShopAddon, out var addon))
        {
            return null;
        }

        var entries = new AddonMaster.InclusionShop((nint)addon).ShopItems;
        for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
        {
            if (entries[entryIndex].ItemId == itemId)
            {
                return entries[entryIndex];
            }
        }

        return null;
    }

    private static AddonMaster.ShopExchangeCurrency.ShopItemInfo? FindCurrencyItem(uint itemId)
    {
        if (!AddonProbe.TryGetReady(CurrencyShopAddon, out var addon))
        {
            return null;
        }

        var entries = new AddonMaster.ShopExchangeCurrency((nint)addon).BasicShopItems;
        for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
        {
            if (entries[entryIndex].ItemId == itemId)
            {
                return entries[entryIndex];
            }
        }

        return null;
    }
}
