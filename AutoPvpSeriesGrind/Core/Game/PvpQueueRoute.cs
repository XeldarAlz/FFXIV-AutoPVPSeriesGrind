namespace AutoPvpSeriesGrind.Core.Game;

// Capture once at run start. Submission and ownership checks must use the same route,
// including the Casual/Ranked distinction within Crystalline Conflict.
internal readonly record struct PvpQueueRoute(byte RouletteId, uint DutyId)
{
    // ContentRoulette rows, verified against XIVAPI: /api/sheet/ContentRoulette/{40,41}?fields=Name.
    private const byte Casual = 40;
    private const byte Ranked = 41;
    private const byte Frontline = 7;

    public static PvpQueueRoute For(MatchType mode, bool ranked) => mode switch
    {
        MatchType.CrystallineConflict => new(ranked ? Ranked : Casual, 0),
        MatchType.Frontline => new(Frontline, 0),
        MatchType.RivalWings => new(0, PvpDuty.HiddenGorgeDutyId),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    public bool Matches(byte rouletteId, uint regularDutyId = 0)
        => RouletteId != 0 ? rouletteId == RouletteId
            : rouletteId == 0 && DutyId != 0 && regularDutyId == DutyId;
}
