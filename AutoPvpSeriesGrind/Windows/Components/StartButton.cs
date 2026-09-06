using AutoPvpSeriesGrind.Core.Localization;
using Dalamud.Interface;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class StartButton
{
    public static bool Draw(string sublabel, bool enabled, string? disabledReason = null, float width = 0f)
        => HeroButton.Draw(FontAwesomeIcon.Play, Loc.T(L.Common.Start), sublabel, Styling.AccentArc, enabled, disabledReason, width);
}
