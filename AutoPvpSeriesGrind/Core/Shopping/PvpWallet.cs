using FFXIVClientStructs.FFXIV.Client.Game;

namespace AutoPvpSeriesGrind.Core.Shopping;

internal enum PvpCurrency : byte
{
    WolfMarks,
    TrophyCrystals,
}

internal static class PvpWallet
{
    public const uint WolfMarkItemId = 25;
    public const uint TrophyCrystalItemId = 36656;
    public const int CurrencyCap = 20_000;

    public static uint ItemIdOf(PvpCurrency currency)
        => currency == PvpCurrency.WolfMarks ? WolfMarkItemId : TrophyCrystalItemId;

    public static int Held(PvpCurrency currency) => HeldCount(ItemIdOf(currency));

    public static unsafe int HeldCount(uint itemId)
    {
        var inventory = InventoryManager.Instance();
        return inventory is null ? 0 : inventory->GetInventoryItemCount(itemId);
    }
}
