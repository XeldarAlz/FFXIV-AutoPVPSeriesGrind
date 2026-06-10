namespace AutoPvpSeriesGrind.Core.Combat;

internal static class ModerateStrategyBaseline
{
    public const int DisengageHpPercent = 35;
    public const int ReengageHpPercent = 65;
    public const int PanicHpPercent = 20;
    public const int FocusRetreatCount = 3;
    public const int FocusRepositionCount = 2;
    public const int PushAdvantage = 1;
    public const int OutnumberMargin = 1;
    public const int MeleeHoldRange = 5;
    public const int MeleeReach = 3;
    public const int RangedStandoff = 18;
    public const int RangedBand = 16;
    public const int EngageRadius = 25;
    public const int LeashRadius = 18;
    public const int CohesionRadius = 18;
    public const int KiteDistance = 16;
    public const int SupportRadius = 22;
    public const int ThreatRadius = 18;
    public const int BurstSensitivityPercent = 30;
    public const int StageStandoff = 18;
    public const int RepositionDistance = 10;
}

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
    private static readonly StrategyProfile DefensivePreset = new(
        DisengageHp: 0.45f, ReengageHp: 0.75f, PanicHp: 0.35f, FocusRetreatCount: 2, FocusRepositionCount: 2,
        MeleeHoldRange: 7f, RangedStandoff: 23f, MeleeReach: 3f, RangedBand: 18f,
        OutnumberMargin: 0, PushAdvantage: 2, EngageRadius: 22f, LeashRadius: 10f, CohesionRadius: 14f, KiteDistance: 22f,
        SupportRadius: 22f, ThreatRadius: 18f, BurstDropPerSec: 0.20f, StageStandoff: 22f, RepositionDistance: 12f);

    private static readonly StrategyProfile AggressivePreset = new(
        DisengageHp: 0.25f, ReengageHp: 0.45f, PanicHp: 0.15f, FocusRetreatCount: 4, FocusRepositionCount: 3,
        MeleeHoldRange: 2f, RangedStandoff: 14f, MeleeReach: 2.5f, RangedBand: 14f,
        OutnumberMargin: 2, PushAdvantage: 0, EngageRadius: 30f, LeashRadius: 28f, CohesionRadius: 24f, KiteDistance: 12f,
        SupportRadius: 26f, ThreatRadius: 22f, BurstDropPerSec: 0.40f, StageStandoff: 14f, RepositionDistance: 8f);

    private static readonly StrategyProfile ModeratePreset = new(
        DisengageHp: ModerateStrategyBaseline.DisengageHpPercent / 100f,
        ReengageHp: ModerateStrategyBaseline.ReengageHpPercent / 100f,
        PanicHp: ModerateStrategyBaseline.PanicHpPercent / 100f,
        FocusRetreatCount: ModerateStrategyBaseline.FocusRetreatCount,
        FocusRepositionCount: ModerateStrategyBaseline.FocusRepositionCount,
        MeleeHoldRange: ModerateStrategyBaseline.MeleeHoldRange,
        RangedStandoff: ModerateStrategyBaseline.RangedStandoff,
        MeleeReach: ModerateStrategyBaseline.MeleeReach,
        RangedBand: ModerateStrategyBaseline.RangedBand,
        OutnumberMargin: ModerateStrategyBaseline.OutnumberMargin,
        PushAdvantage: ModerateStrategyBaseline.PushAdvantage,
        EngageRadius: ModerateStrategyBaseline.EngageRadius,
        LeashRadius: ModerateStrategyBaseline.LeashRadius,
        CohesionRadius: ModerateStrategyBaseline.CohesionRadius,
        KiteDistance: ModerateStrategyBaseline.KiteDistance,
        SupportRadius: ModerateStrategyBaseline.SupportRadius,
        ThreatRadius: ModerateStrategyBaseline.ThreatRadius,
        BurstDropPerSec: ModerateStrategyBaseline.BurstSensitivityPercent / 100f,
        StageStandoff: ModerateStrategyBaseline.StageStandoff,
        RepositionDistance: ModerateStrategyBaseline.RepositionDistance);

    private const float TankHoldRangeFactor = 0.5f;
    private const float TankRetreatHpDelta = 0.10f;
    private const float TankPanicHpDelta = 0.05f;
    private const float MinTankRetreatHp = 0.10f;
    private const float MinTankPanicHp = 0.05f;

    private const float HealerStandoffBonus = 4f;
    private const float HealerKiteBonus = 4f;
    private const float HealerCohesionTightening = 4f;
    private const float MinHealerCohesion = 8f;
    private const float HealerRetreatHpDelta = 0.10f;
    private const float HealerReengageHpDelta = 0.05f;
    private const float MaxHealerRetreatHp = 0.90f;
    private const float MaxHealerReengageHp = 0.95f;

    public static StrategyProfile For(PvpStrategy strategy, CustomStrategyProfile? custom = null) => strategy switch
    {
        PvpStrategy.Defensive => DefensivePreset,
        PvpStrategy.Aggressive => AggressivePreset,
        PvpStrategy.Custom => (custom ?? new CustomStrategyProfile()).ToProfile(),
        PvpStrategy.Moderate => ModeratePreset,
        _ => ModeratePreset,
    };

    public StrategyProfile WithRole(PvpRole role) => role switch
    {
        PvpRole.Tank => WithTankRole(),
        PvpRole.Healer => WithHealerRole(),
        _ => this,
    };

    private StrategyProfile WithTankRole() => this with
    {
        MeleeHoldRange = MeleeHoldRange * TankHoldRangeFactor,
        DisengageHp = MathF.Max(MinTankRetreatHp, DisengageHp - TankRetreatHpDelta),
        PanicHp = MathF.Max(MinTankPanicHp, PanicHp - TankPanicHpDelta),
        FocusRetreatCount = FocusRetreatCount + 1,
        FocusRepositionCount = FocusRepositionCount + 1,
        OutnumberMargin = OutnumberMargin + 1,
    };

    private StrategyProfile WithHealerRole() => this with
    {
        RangedStandoff = RangedStandoff + HealerStandoffBonus,
        RangedBand = RangedBand + HealerStandoffBonus,
        KiteDistance = KiteDistance + HealerKiteBonus,
        CohesionRadius = MathF.Max(MinHealerCohesion, CohesionRadius - HealerCohesionTightening),
        DisengageHp = MathF.Min(MaxHealerRetreatHp, DisengageHp + HealerRetreatHpDelta),
        ReengageHp = MathF.Min(MaxHealerReengageHp, ReengageHp + HealerReengageHpDelta),
    };
}
