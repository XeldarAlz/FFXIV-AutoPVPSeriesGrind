namespace AutoPvpSeriesGrind.Core.Combat;

public sealed class CustomStrategyProfile
{
    public int DisengageHpPercent { get; set; } = 30;
    public int ReengageHpPercent { get; set; } = 60;
    public int PanicHpPercent { get; set; } = 18;
    public int FocusRetreatCount { get; set; } = 3;

    public int PushAdvantage { get; set; } = 1;
    public int OutnumberMargin { get; set; } = 2;

    public int MeleeHoldRange { get; set; } = 4;
    public int MeleeReach { get; set; } = 3;
    public int RangedStandoff { get; set; } = 17;
    public int RangedBand { get; set; } = 16;

    public int EngageRadius { get; set; } = 25;
    public int LeashRadius { get; set; } = 18;
    public int CohesionRadius { get; set; } = 20;
    public int KiteDistance { get; set; } = 16;

    internal StrategyProfile ToProfile() => new(
        DisengageHp: Pct(DisengageHpPercent),
        ReengageHp: Pct(ReengageHpPercent),
        PanicHp: Pct(PanicHpPercent),
        FocusRetreatCount: Math.Max(1, FocusRetreatCount),
        MeleeHoldRange: NonNeg(MeleeHoldRange),
        RangedStandoff: NonNeg(RangedStandoff),
        MeleeReach: NonNeg(MeleeReach),
        RangedBand: NonNeg(RangedBand),
        OutnumberMargin: Math.Max(1, OutnumberMargin),
        PushAdvantage: PushAdvantage,
        EngageRadius: NonNeg(EngageRadius),
        LeashRadius: NonNeg(LeashRadius),
        CohesionRadius: NonNeg(CohesionRadius),
        KiteDistance: NonNeg(KiteDistance));

    private static float Pct(int percent) => Math.Clamp(percent, 0, 100) / 100f;
    private static float NonNeg(int yalms) => Math.Max(0, yalms);
}
