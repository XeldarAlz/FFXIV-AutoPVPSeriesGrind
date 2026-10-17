using AutoPvpSeriesGrind.Core.Localization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

// Measured, wrapping cards: labels and descriptions remain visible at larger UI scales
// and in translations. The entire card is one target, with a non-color selection mark.
internal static class ChoiceCards
{
    public readonly record struct Item(LocString Title, LocString Detail, FontAwesomeIcon Icon, Vector4 Accent);

    public static bool Draw(string id, ReadOnlySpan<Item> items, ref int selected,
        int maxColumns = 3, float minWidth = 190f, bool enabled = true)
    {
        if (items.Length == 0) return false;
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var gap = 10f * scale;
        var pad = 14f * scale;
        var columns = Math.Clamp((int)((width + gap) / (minWidth * scale + gap)), 1, Math.Min(items.Length, maxColumns));
        var cardWidth = (width - gap * (columns - 1)) / columns;
        var textWidth = MathF.Max(1f, cardWidth - pad * 2f);
        var y = origin.Y;
        var changed = false;
        using var scope = ImRaii.PushId(id);

        for (var row = 0; row < items.Length; row += columns)
        {
            var count = Math.Min(columns, items.Length - row);
            var height = 0f;
            for (var col = 0; col < count; col++)
            {
                var item = items[row + col];
                float titleHeight;
                using (Fonts.PushHeadline()) titleHeight = TextDraw.MeasureWrapped(Loc.T(item.Title), textWidth).Y;
                using (Fonts.PushCaption())
                    height = MathF.Max(height, pad * 2f + 30f * scale + titleHeight + 5f * scale + TextDraw.MeasureWrapped(Loc.T(item.Detail), textWidth).Y);
            }

            for (var col = 0; col < count; col++)
            {
                var index = row + col;
                var item = items[index];
                var min = new Vector2(origin.X + col * (cardWidth + gap), y);
                var max = min + new Vector2(cardWidth, height);
                ImGui.SetCursorScreenPos(min);
                using var cardId = ImRaii.PushId(index);
                var hit = Hit.Area("##card", max - min, enabled);
                var hover = Motion.Hover(Motion.Key("##card"), hit.Hovered);
                if (hit.Clicked && selected != index)
                {
                    selected = index;
                    changed = true;
                }

                var active = selected == index;
                var dl = ImGui.GetWindowDrawList();
                var accent = enabled ? item.Accent : Styling.TextDim;
                Paint.Glass(dl, min, max, 14f * scale, accent, active ? 0.18f : 0.035f, hover);
                if (active) Paint.Stroke(dl, min, max, Styling.WithAlpha(accent, 0.85f), 14f * scale, 1.5f * scale);
                var iconCenter = min + new Vector2(pad + 11f * scale, pad + 10f * scale);
                dl.AddCircleFilled(iconCenter, 16f * scale, Paint.Col(Styling.WithAlpha(accent, 0.14f)), 24);
                TextDraw.IconCentered(item.Icon, iconCenter, Styling.Lighten(accent, 0.25f));
                if (active) TextDraw.IconRight(FontAwesomeIcon.CheckCircle, max.X - pad, iconCenter.Y, accent);

                var textPos = min + new Vector2(pad, pad + 30f * scale);
                using (Fonts.PushHeadline())
                {
                    TextDraw.Wrapped(Loc.T(item.Title), textPos, textWidth, enabled ? Styling.TextStrong : Styling.TextDim);
                    textPos.Y += TextDraw.MeasureWrapped(Loc.T(item.Title), textWidth).Y + 5f * scale;
                }
                using (Fonts.PushCaption())
                    TextDraw.Wrapped(Loc.T(item.Detail), textPos, textWidth, Styling.TextSecondary);
            }
            y += height + gap;
        }
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, y - origin.Y - gap));
        return changed;
    }

    public static void Description(string text, Vector4? color = null)
    {
        var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        using (Fonts.PushCaption())
        {
            TextDraw.Wrapped(text, ImGui.GetCursorScreenPos(), width, color ?? Styling.TextSecondary);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(text, width).Y));
        }
    }
}
