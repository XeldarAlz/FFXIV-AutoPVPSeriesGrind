using AutoPvpSeriesGrind.Core.Localization;
using Dalamud.Interface;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class StopButton
{
    public static bool Draw(string? sublabel, float width = 0f)
        => HeroButton.Draw(FontAwesomeIcon.Stop, Loc.T(L.Common.Stop), sublabel, Styling.AccentRose, true, null, width);
}
