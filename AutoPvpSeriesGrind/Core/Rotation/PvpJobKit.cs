namespace AutoPvpSeriesGrind.Core.Rotation;

internal sealed class PvpJobKit
{
    public required uint JobId { get; init; }
    public required PvpActionInfo[] Abilities { get; init; }
    public required PvpActionInfo[] CooldownGcds { get; init; }
    public required PvpActionInfo[] FillerGcds { get; init; }
    public required byte GcdCooldownGroup { get; init; }

    public bool IsEmpty => Abilities.Length == 0 && CooldownGcds.Length == 0 && FillerGcds.Length == 0;
}
