using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

internal static class Layout
{
    public const float PrimaryButtonHeight = 44f;
    public const float HeroRingRadius = 58f;
    public const float HeroRingIconRatio = 0.62f;
    public const string WideDotSeparator = "     ·     ";

    public static readonly Vector4 RingTrackColor = Styling.WithAlpha(Styling.BorderDim, 0.85f);
}
