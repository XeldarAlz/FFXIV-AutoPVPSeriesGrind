namespace AutoPvpSeriesGrind.Core.Rotation;

internal enum PvpActionSlot : byte
{
    CooldownGcd,
    Ability,
    FillerGcd,
}

internal readonly record struct PvpActionInfo(
    uint Id,
    string Name,
    PvpActionSlot Slot,
    bool TargetsHostile,
    bool TargetsAlly,
    bool TargetsSelf,
    bool TargetArea,
    bool HasCastTime,
    sbyte Range,
    byte EffectRange,
    byte CooldownGroup)
{
    public bool IsGcd => Slot != PvpActionSlot.Ability;
}
