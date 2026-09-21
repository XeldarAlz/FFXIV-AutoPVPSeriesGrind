namespace AutoPvpSeriesGrind.Core.Rotation;

internal readonly record struct RotationSettings(
    float GuardHp,
    bool GuardOnBurst,
    float GuardOnBurstHp,
    uint RecuperateMissingHp,
    float ElixirResourceFraction,
    float AllySupportHp,
    bool Purify)
{
    private const float PercentToFraction = 100f;

    public static RotationSettings From(Configuration cfg) => new(
        GuardHp: cfg.RotationGuardHpPercent / PercentToFraction,
        GuardOnBurst: cfg.RotationGuardOnBurst,
        GuardOnBurstHp: cfg.RotationGuardOnBurstHpPercent / PercentToFraction,
        RecuperateMissingHp: (uint)Math.Max(0, cfg.RotationRecuperateMissingHp),
        ElixirResourceFraction: cfg.RotationElixirPercent / PercentToFraction,
        AllySupportHp: cfg.RotationAllySupportHpPercent / PercentToFraction,
        Purify: cfg.RotationPurify);
}
