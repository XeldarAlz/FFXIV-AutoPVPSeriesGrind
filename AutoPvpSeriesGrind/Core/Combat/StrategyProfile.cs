namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed record StrategyProfile(
    float DisengageHp,
    float ReengageHp,
    float PanicHp,
    int FocusRetreatCount,
    int FocusRepositionCount,
    float MeleeHoldRange,
    float RangedStandoff,
    float MeleeReach,
    float RangedBand,
    int OutnumberMargin,
    int PushAdvantage,
    float EngageRadius,
    float LeashRadius,
    float CohesionRadius,
    float KiteDistance,
    float SupportRadius,
    float ThreatRadius,
    float BurstDropPerSec,
    float StageStandoff,
    float RepositionDistance)
{
    public static StrategyProfile For(PvpStrategy strategy, CustomStrategyProfile? custom = null) => strategy switch
    {
        PvpStrategy.Defensive => new(DisengageHp: 0.55f, ReengageHp: 0.85f, PanicHp: 0.35f, FocusRetreatCount: 2, FocusRepositionCount: 1,
            MeleeHoldRange: 7f, RangedStandoff: 23f, MeleeReach: 3f, RangedBand: 18f,
            OutnumberMargin: 0, PushAdvantage: 2, EngageRadius: 22f, LeashRadius: 10f, CohesionRadius: 14f, KiteDistance: 22f,
            SupportRadius: 22f, ThreatRadius: 18f, BurstDropPerSec: 0.20f, StageStandoff: 22f, RepositionDistance: 12f),

        PvpStrategy.Aggressive => new(DisengageHp: 0.18f, ReengageHp: 0.45f, PanicHp: 0.10f, FocusRetreatCount: 5, FocusRepositionCount: 3,
            MeleeHoldRange: 2f, RangedStandoff: 14f, MeleeReach: 2.5f, RangedBand: 14f,
            OutnumberMargin: 2, PushAdvantage: 0, EngageRadius: 30f, LeashRadius: 28f, CohesionRadius: 24f, KiteDistance: 12f,
            SupportRadius: 26f, ThreatRadius: 22f, BurstDropPerSec: 0.40f, StageStandoff: 14f, RepositionDistance: 8f),

        PvpStrategy.Custom => (custom ?? new CustomStrategyProfile()).ToProfile(),

        _ => new(DisengageHp: 0.35f, ReengageHp: 0.65f, PanicHp: 0.20f, FocusRetreatCount: 3, FocusRepositionCount: 2,
            MeleeHoldRange: 5f, RangedStandoff: 18f, MeleeReach: 3f, RangedBand: 16f,
            OutnumberMargin: 1, PushAdvantage: 1, EngageRadius: 25f, LeashRadius: 18f, CohesionRadius: 18f, KiteDistance: 16f,
            SupportRadius: 22f, ThreatRadius: 18f, BurstDropPerSec: 0.30f, StageStandoff: 18f, RepositionDistance: 10f),
    };
}
