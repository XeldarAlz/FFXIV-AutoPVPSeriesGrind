namespace AutoPvpSeriesGrind.Core.Rotation;

internal enum RuleWhen : byte
{
    SelfHasStatus,
    SelfLacksStatus,
    SelfHasAnyStatus,
    TargetHasStatus,
    TargetLacksStatus,
    SelfHpBelow,
    SelfHpAbove,
    TargetHpBelow,
    EnemiesWithin,
    InCombat,
    SelfStatusEndingWithin,
    SelfStatusStacksAtLeast,
    SelfStatusStacksAtMost,
    ChargesAtLeast,
    NotLastUsed,
    TargetWithin,
    TargetBeyond,
    AllyBelow,
}

internal readonly record struct RuleCondition(RuleWhen Kind, uint Status = 0, float Value = 0f, uint[]? Statuses = null);

internal enum RuleTarget : byte
{
    Auto,
    Self,
    NearestAlly,
}

internal readonly record struct RotationRule(uint Button, uint Adjusted, RuleTarget Target, RuleCondition[] Conditions);

internal sealed class RotationTable(uint pauseWhileSelfHas, RotationRule[] rules)
{
    public uint PauseWhileSelfHas { get; } = pauseWhileSelfHas;
    public RotationRule[] Rules { get; } = rules;
}

internal static class Rule
{
    public static RotationRule Use(uint button, params RuleCondition[] when) => new(button, 0, RuleTarget.Auto, when);

    public static RotationRule UseAs(uint button, uint adjusted, params RuleCondition[] when) => new(button, adjusted, RuleTarget.Auto, when);

    public static RotationRule UseOnSelf(uint button, params RuleCondition[] when) => new(button, 0, RuleTarget.Self, when);

    public static RotationRule UseAsOnSelf(uint button, uint adjusted, params RuleCondition[] when) => new(button, adjusted, RuleTarget.Self, when);

    public static RotationRule UseOnAlly(uint button, params RuleCondition[] when) => new(button, 0, RuleTarget.NearestAlly, when);
}

internal static class When
{
    public static readonly RuleCondition InCombat = new(RuleWhen.InCombat);
    public static readonly RuleCondition NotLastUsed = new(RuleWhen.NotLastUsed);

    public static RuleCondition SelfHas(uint status) => new(RuleWhen.SelfHasStatus, status);
    public static RuleCondition SelfLacks(uint status) => new(RuleWhen.SelfLacksStatus, status);
    public static RuleCondition SelfHasAny(uint[] statuses) => new(RuleWhen.SelfHasAnyStatus, Statuses: statuses);
    public static RuleCondition TargetHas(uint status) => new(RuleWhen.TargetHasStatus, status);
    public static RuleCondition TargetLacks(uint status) => new(RuleWhen.TargetLacksStatus, status);
    public static RuleCondition SelfHpBelow(float fraction) => new(RuleWhen.SelfHpBelow, Value: fraction);
    public static RuleCondition SelfHpAbove(float fraction) => new(RuleWhen.SelfHpAbove, Value: fraction);
    public static RuleCondition TargetHpBelow(float fraction) => new(RuleWhen.TargetHpBelow, Value: fraction);
    public static RuleCondition EnemiesWithin(float yalms) => new(RuleWhen.EnemiesWithin, Value: yalms);
    public static RuleCondition SelfStatusEnding(uint status, float seconds) => new(RuleWhen.SelfStatusEndingWithin, status, seconds);
    public static RuleCondition SelfStacksAtLeast(uint status, int stacks) => new(RuleWhen.SelfStatusStacksAtLeast, status, stacks);
    public static RuleCondition SelfStacksAtMost(uint status, int stacks) => new(RuleWhen.SelfStatusStacksAtMost, status, stacks);
    public static RuleCondition ChargesAtLeast(int charges) => new(RuleWhen.ChargesAtLeast, Value: charges);
    public static RuleCondition TargetWithin(float yalms) => new(RuleWhen.TargetWithin, Value: yalms);
    public static RuleCondition TargetBeyond(float yalms) => new(RuleWhen.TargetBeyond, Value: yalms);
    public static RuleCondition AllyBelow(float fraction) => new(RuleWhen.AllyBelow, Value: fraction);
}
