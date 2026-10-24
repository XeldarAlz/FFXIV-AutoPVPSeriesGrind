using Dalamud.Game.ClientState.Objects.Types;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal static class HpMath
{
    private const float PercentScale = 100f;

    // ShieldPercentage is 0 to 100, as a share of MaxHp.
    public static uint EffectiveHp(IBattleChara target)
        => target.CurrentHp + (uint)((ulong)target.MaxHp * target.ShieldPercentage / 100UL);

    public static float EffectiveHpPercent(IBattleChara target)
        => target.MaxHp == 0 ? PercentScale : PercentScale * EffectiveHp(target) / target.MaxHp;

    public static bool IsBelowThreshold(IBattleChara target, LbRule rule)
        => rule.EnemyHpMode == ThresholdMode.Absolute
            ? EffectiveHp(target) < rule.EnemyHpAbsolute
            : target.MaxHp != 0 && EffectiveHpPercent(target) < rule.EnemyHpPercent;
}
