namespace AutoPvpSeriesGrind.Core.Combat;

public sealed class CustomStrategyProfile
{
    public int DisengageHpPercent { get; set; } = 35;
    public int ReengageHpPercent { get; set; } = 65;
    public int PanicHpPercent { get; set; } = 20;
    public int FocusRetreatCount { get; set; } = 3;
    public int FocusRepositionCount { get; set; } = 2;

    public int PushAdvantage { get; set; } = 1;
    public int OutnumberMargin { get; set; } = 1;

    public int MeleeHoldRange { get; set; } = 5;
    public int MeleeReach { get; set; } = 3;
    public int RangedStandoff { get; set; } = 18;
    public int RangedBand { get; set; } = 16;

    public int EngageRadius { get; set; } = 25;
    public int LeashRadius { get; set; } = 18;
    public int CohesionRadius { get; set; } = 18;
    public int KiteDistance { get; set; } = 16;

    public int SupportRadius { get; set; } = 22;
    public int ThreatRadius { get; set; } = 18;
    public int BurstSensitivityPercent { get; set; } = 30;
    public int StageStandoff { get; set; } = 18;
    public int RepositionDistance { get; set; } = 10;

    internal StrategyProfile ToProfile() => new(
        DisengageHp: Pct(DisengageHpPercent),
        ReengageHp: Pct(ReengageHpPercent),
        PanicHp: Pct(PanicHpPercent),
        FocusRetreatCount: Math.Max(1, FocusRetreatCount),
        FocusRepositionCount: Math.Max(1, FocusRepositionCount),
        MeleeHoldRange: NonNeg(MeleeHoldRange),
        RangedStandoff: NonNeg(RangedStandoff),
        MeleeReach: NonNeg(MeleeReach),
        RangedBand: NonNeg(RangedBand),
        OutnumberMargin: Math.Max(0, OutnumberMargin),
        PushAdvantage: PushAdvantage,
        EngageRadius: NonNeg(EngageRadius),
        LeashRadius: NonNeg(LeashRadius),
        CohesionRadius: NonNeg(CohesionRadius),
        KiteDistance: NonNeg(KiteDistance),
        SupportRadius: NonNeg(SupportRadius),
        ThreatRadius: NonNeg(ThreatRadius),
        BurstDropPerSec: Math.Clamp(BurstSensitivityPercent, 5, 100) / 100f,
        StageStandoff: NonNeg(StageStandoff),
        RepositionDistance: NonNeg(RepositionDistance));

    private static float Pct(int percent) => Math.Clamp(percent, 0, 100) / 100f;
    private static float NonNeg(int yalms) => Math.Max(0, yalms);
}
