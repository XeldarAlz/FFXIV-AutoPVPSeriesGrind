namespace AutoPvpSeriesGrind.Core.Rotation;

internal enum PvpActionSlot : byte
{
    Ability,
    CooldownGcd,
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
    byte CooldownGroup)
{
    public bool IsAllySupport => TargetsAlly && !TargetsHostile;
}
