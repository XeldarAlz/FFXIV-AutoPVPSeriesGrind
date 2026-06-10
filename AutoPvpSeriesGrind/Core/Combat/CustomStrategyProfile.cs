namespace AutoPvpSeriesGrind.Core.Combat;

public sealed class CustomStrategyProfile
{
    public int DisengageHpPercent { get; set; } = ModerateStrategyBaseline.DisengageHpPercent;
    public int ReengageHpPercent { get; set; } = ModerateStrategyBaseline.ReengageHpPercent;
    public int PanicHpPercent { get; set; } = ModerateStrategyBaseline.PanicHpPercent;
    public int FocusRetreatCount { get; set; } = ModerateStrategyBaseline.FocusRetreatCount;
    public int FocusRepositionCount { get; set; } = ModerateStrategyBaseline.FocusRepositionCount;

    public int PushAdvantage { get; set; } = ModerateStrategyBaseline.PushAdvantage;
    public int OutnumberMargin { get; set; } = ModerateStrategyBaseline.OutnumberMargin;

    public int MeleeHoldRange { get; set; } = ModerateStrategyBaseline.MeleeHoldRange;
    public int MeleeReach { get; set; } = ModerateStrategyBaseline.MeleeReach;
    public int RangedStandoff { get; set; } = ModerateStrategyBaseline.RangedStandoff;
    public int RangedBand { get; set; } = ModerateStrategyBaseline.RangedBand;

    public int EngageRadius { get; set; } = ModerateStrategyBaseline.EngageRadius;
    public int LeashRadius { get; set; } = ModerateStrategyBaseline.LeashRadius;
    public int CohesionRadius { get; set; } = ModerateStrategyBaseline.CohesionRadius;
    public int KiteDistance { get; set; } = ModerateStrategyBaseline.KiteDistance;

    public int SupportRadius { get; set; } = ModerateStrategyBaseline.SupportRadius;
    public int ThreatRadius { get; set; } = ModerateStrategyBaseline.ThreatRadius;
    public int BurstSensitivityPercent { get; set; } = ModerateStrategyBaseline.BurstSensitivityPercent;
    public int StageStandoff { get; set; } = ModerateStrategyBaseline.StageStandoff;
    public int RepositionDistance { get; set; } = ModerateStrategyBaseline.RepositionDistance;

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
