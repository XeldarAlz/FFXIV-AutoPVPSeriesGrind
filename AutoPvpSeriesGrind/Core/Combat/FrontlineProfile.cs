namespace AutoPvpSeriesGrind.Core.Combat;

internal static class ModerateFrontlineBaseline
{
    public const int FrontOffset = 6;
    public const int BackOffset = 6;
    public const int RetreatOffset = 12;
    public const int RegroupDistance = 25;
    public const int MountDistance = 60;
    public const int EnemyAwareRadius = 30;
    public const int EngageRange = 25;
    public const int HurtHpPercent = 45;
    public const int FocusedHurtHpPercent = 60;
    public const int RecoveredHpPercent = 70;
    public const int FocusHurtCount = 2;
    public const int BurstSensitivityPercent = 30;
}

internal sealed record FrontlineProfile(
    float FrontOffset,
    float BackOffset,
    float RetreatOffset,
    float RegroupDistance,
    float MountDistance,
    float EnemyAwareRadius,
    float EngageRange,
    float HurtHp,
    float FocusedHurtHp,
    float RecoveredHp,
    int FocusHurtCount,
    float BurstDropPerSec)
{
    private static readonly FrontlineProfile DefensivePreset = new(
        FrontOffset: 3f, BackOffset: 8f, RetreatOffset: 16f, RegroupDistance: 18f, MountDistance: 60f, EnemyAwareRadius: 35f,
        EngageRange: 25f, HurtHp: 0.55f, FocusedHurtHp: 0.70f, RecoveredHp: 0.80f, FocusHurtCount: 2, BurstDropPerSec: 0.20f);

    private static readonly FrontlineProfile AggressivePreset = new(
        FrontOffset: 10f, BackOffset: 3f, RetreatOffset: 8f, RegroupDistance: 32f, MountDistance: 60f, EnemyAwareRadius: 25f,
        EngageRange: 25f, HurtHp: 0.30f, FocusedHurtHp: 0.45f, RecoveredHp: 0.55f, FocusHurtCount: 3, BurstDropPerSec: 0.40f);

    private static readonly FrontlineProfile ModeratePreset = new(
        FrontOffset: ModerateFrontlineBaseline.FrontOffset,
        BackOffset: ModerateFrontlineBaseline.BackOffset,
        RetreatOffset: ModerateFrontlineBaseline.RetreatOffset,
        RegroupDistance: ModerateFrontlineBaseline.RegroupDistance,
        MountDistance: ModerateFrontlineBaseline.MountDistance,
        EnemyAwareRadius: ModerateFrontlineBaseline.EnemyAwareRadius,
        EngageRange: ModerateFrontlineBaseline.EngageRange,
        HurtHp: ModerateFrontlineBaseline.HurtHpPercent / 100f,
        FocusedHurtHp: ModerateFrontlineBaseline.FocusedHurtHpPercent / 100f,
        RecoveredHp: ModerateFrontlineBaseline.RecoveredHpPercent / 100f,
        FocusHurtCount: ModerateFrontlineBaseline.FocusHurtCount,
        BurstDropPerSec: ModerateFrontlineBaseline.BurstSensitivityPercent / 100f);

    public static FrontlineProfile For(PvpStrategy strategy, CustomFrontlineProfile? custom = null) => strategy switch
    {
        PvpStrategy.Defensive => DefensivePreset,
        PvpStrategy.Aggressive => AggressivePreset,
        PvpStrategy.Custom => (custom ?? new CustomFrontlineProfile()).ToProfile(),
        _ => ModeratePreset,
    };
}
