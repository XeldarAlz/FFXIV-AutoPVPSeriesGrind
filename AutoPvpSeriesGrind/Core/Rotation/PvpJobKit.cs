namespace AutoPvpSeriesGrind.Core.Rotation;

internal sealed class PvpJobKit
{
    public required uint JobId { get; init; }
    public required PvpActionInfo[] Buttons { get; init; }
    public required byte GcdCooldownGroup { get; init; }
}
