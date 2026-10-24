namespace AutoPvpSeriesGrind.Core.LimitBreak;

public enum LbFireMode
{
    Offensive,
    Defensive,
    Utility,
}

public enum ThresholdMode
{
    Percent,
    Absolute,
}

public sealed class LbRule
{
    public LbFireMode Mode { get; set; } = LbFireMode.Offensive;

    public ThresholdMode EnemyHpMode { get; set; } = ThresholdMode.Percent;
    public float EnemyHpPercent { get; set; } = 30f;
    public uint EnemyHpAbsolute { get; set; } = 7000;

    public float AllyHpPercent { get; set; } = 50f;
    public int AllyCountNear { get; set; } = 2;
    public float AllyRadiusYalms { get; set; } = 15f;
    public int EnemyCountNear { get; set; } = 1;
    public float EnemyRadiusYalms { get; set; } = 20f;

    public LbRule Clone() => (LbRule)MemberwiseClone();
}
