namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed record StrategyProfile(
    float DisengageHp,
    float ReengageHp,
    float PanicHp,
    int FocusRetreatCount,
    float MeleeHoldRange,
    float RangedStandoff,
    float MeleeReach,
    float RangedBand,
    int OutnumberMargin,
    int PushAdvantage,
    float EngageRadius,
    float LeashRadius,
    float CohesionRadius,
    float KiteDistance)
{
    public static StrategyProfile For(PvpStrategy strategy, CustomStrategyProfile? custom = null) => strategy switch
    {
        PvpStrategy.Defensive => new(DisengageHp: 0.50f, ReengageHp: 0.80f, PanicHp: 0.30f, FocusRetreatCount: 2,
            MeleeHoldRange: 6f, RangedStandoff: 22f, MeleeReach: 3f, RangedBand: 18f,
            OutnumberMargin: 1, PushAdvantage: 2, EngageRadius: 22f, LeashRadius: 10f, CohesionRadius: 16f, KiteDistance: 20f),

        PvpStrategy.Aggressive => new(DisengageHp: 0.15f, ReengageHp: 0.45f, PanicHp: 0.10f, FocusRetreatCount: 5,
            MeleeHoldRange: 2f, RangedStandoff: 13f, MeleeReach: 2.5f, RangedBand: 14f,
            OutnumberMargin: 3, PushAdvantage: 0, EngageRadius: 30f, LeashRadius: 28f, CohesionRadius: 26f, KiteDistance: 12f),

        PvpStrategy.Custom => (custom ?? new CustomStrategyProfile()).ToProfile(),

        _ => new(DisengageHp: 0.30f, ReengageHp: 0.60f, PanicHp: 0.18f, FocusRetreatCount: 3,
            MeleeHoldRange: 4f, RangedStandoff: 17f, MeleeReach: 3f, RangedBand: 16f,
            OutnumberMargin: 2, PushAdvantage: 1, EngageRadius: 25f, LeashRadius: 18f, CohesionRadius: 20f, KiteDistance: 16f),
    };
}
