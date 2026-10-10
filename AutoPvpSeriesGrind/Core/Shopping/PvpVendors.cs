using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Shopping;

internal sealed record PvpVendor(PvpCurrency Currency, uint EnpcBaseId, Vector3 Position);

internal static class PvpVendors
{
    public const uint WolvesDenPierTerritoryId = 250;

    public static readonly PvpVendor MarkQuartermaster =
        new(PvpCurrency.WolfMarks, 1005244, new Vector3(-4.896f, 2.057f, -5.539f));

    public static readonly PvpVendor CrystalQuartermaster =
        new(PvpCurrency.TrophyCrystals, 1038441, new Vector3(-4.898f, 2.057f, -0.504f));

    public static PvpVendor For(PvpCurrency currency)
        => currency == PvpCurrency.WolfMarks ? MarkQuartermaster : CrystalQuartermaster;
}
