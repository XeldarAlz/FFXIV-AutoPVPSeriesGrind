using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class Chip
{
    private const float PadX = 11f;
    private const float DotRadius = 3f;
    private const float IconGap = 6f;

    public static void Draw(string label, Vector4 accent, FontAwesomeIcon? icon = null, bool dot = false,
        bool pulse = false, string? tooltip = null)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var padX = PadX * scale;
        var iconGap = IconGap * scale;
        var textSize = TextDraw.Measure(label);
        var iconWidth = icon is { } glyph ? TextDraw.IconSize(glyph).X + iconGap : 0f;
        var dotWidth = dot ? DotRadius * 2f * scale + iconGap : 0f;
        var size = new Vector2(padX * 2f + iconWidth + dotWidth + textSize.X, Layout.ChipHeight * scale);

        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        Paint.Pill(dl, origin, end, Styling.WithAlpha(accent, 0.14f), Styling.WithAlpha(accent, 0.42f));

        var midY = origin.Y + size.Y * 0.5f;
        var x = origin.X + padX;

        if (dot)
        {
            var radius = DotRadius * scale;
            var alpha = pulse ? 0.45f + 0.55f * Styling.Pulse(Styling.PulseCalm) : 1f;
            dl.AddCircleFilled(new Vector2(x + radius, midY), radius, Paint.Col(Styling.WithAlpha(accent, alpha)));
            x += radius * 2f + iconGap;
        }

        if (icon is { } iconGlyph)
        {
            var iconSize = TextDraw.IconSize(iconGlyph);
            TextDraw.Icon(iconGlyph, new Vector2(x, midY - iconSize.Y * 0.5f), accent);
            x += iconSize.X + iconGap;
        }

        TextDraw.At(label, new Vector2(x, midY - textSize.Y * 0.5f), Styling.TextSecondary);

        ImGui.Dummy(size);
        if (tooltip is not null && ImGui.IsItemHovered()) Tooltip.Show(tooltip);
    }
}
