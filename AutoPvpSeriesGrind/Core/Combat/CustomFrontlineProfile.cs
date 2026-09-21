namespace AutoPvpSeriesGrind.Core.Combat;

public sealed class CustomFrontlineProfile
{
    public int FrontOffset { get; set; } = ModerateFrontlineBaseline.FrontOffset;
    public int BackOffset { get; set; } = ModerateFrontlineBaseline.BackOffset;
    public int RetreatOffset { get; set; } = ModerateFrontlineBaseline.RetreatOffset;
    public int RegroupDistance { get; set; } = ModerateFrontlineBaseline.RegroupDistance;
    public int MountDistance { get; set; } = ModerateFrontlineBaseline.MountDistance;
    public int EnemyAwareRadius { get; set; } = ModerateFrontlineBaseline.EnemyAwareRadius;
    public int EngageRange { get; set; } = ModerateFrontlineBaseline.EngageRange;
    public int HurtHpPercent { get; set; } = ModerateFrontlineBaseline.HurtHpPercent;
    public int FocusedHurtHpPercent { get; set; } = ModerateFrontlineBaseline.FocusedHurtHpPercent;
    public int RecoveredHpPercent { get; set; } = ModerateFrontlineBaseline.RecoveredHpPercent;
    public int FocusHurtCount { get; set; } = ModerateFrontlineBaseline.FocusHurtCount;
    public int BurstSensitivityPercent { get; set; } = ModerateFrontlineBaseline.BurstSensitivityPercent;

    internal FrontlineProfile ToProfile() => new(
        FrontOffset: NonNeg(FrontOffset),
        BackOffset: NonNeg(BackOffset),
        RetreatOffset: NonNeg(RetreatOffset),
        RegroupDistance: Math.Max(5, RegroupDistance),
        MountDistance: Math.Max(10, MountDistance),
        EnemyAwareRadius: NonNeg(EnemyAwareRadius),
        EngageRange: Math.Max(5, EngageRange),
        HurtHp: Pct(HurtHpPercent),
        FocusedHurtHp: Pct(FocusedHurtHpPercent),
        RecoveredHp: Pct(RecoveredHpPercent),
        FocusHurtCount: Math.Max(1, FocusHurtCount),
        BurstDropPerSec: Math.Clamp(BurstSensitivityPercent, 5, 100) / 100f);

    private static float Pct(int percent) => Math.Clamp(percent, 0, 100) / 100f;
    private static float NonNeg(int yalms) => Math.Max(0, yalms);
}
