using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class SegmentButton
{
    public static bool Draw(string id, FontAwesomeIcon icon, string label, Vector4 accent,
        bool selected, bool disabled, Vector2 size, string? tooltip)
    {
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var rawHovered = ImGui.IsMouseHoveringRect(origin, end);
        var hovered = !disabled && rawHovered;
        var scale = ImGuiHelpers.GlobalScale;

        var background = disabled ? Styling.CardBgSoft
            : selected ? Vector4.Lerp(Styling.CardBg, accent, 0.22f)
            : hovered ? Styling.CardBgHover : Styling.CardBgSoft;
        var border = disabled ? Styling.BorderDim
            : selected ? accent : hovered ? accent * 0.5f : Styling.BorderDim;
        var textColor = disabled ? Styling.TextMuted : selected ? Styling.TextStrong : Styling.TextSecondary;
        var iconColor = disabled ? Styling.TextMuted : selected ? accent : Styling.TextSecondary;

        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(background), 6f);
        drawList.AddRect(origin, end, ImGui.GetColorU32(border), 6f, ImDrawFlags.None, selected ? 2f : 1f);

        var iconString = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            iconSize = ImGui.CalcTextSize(iconString);
        }

        var labelSize = ImGui.CalcTextSize(label);
        var innerGap = 6f * scale;
        var contentWidth = iconSize.X + innerGap + labelSize.X;
        var contentStartX = origin.X + MathF.Max(4f * scale, (size.X - contentWidth) * 0.5f);
        var middleY = origin.Y + size.Y * 0.5f;

        ImGui.SetCursorScreenPos(new Vector2(contentStartX, middleY - iconSize.Y * 0.5f));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, iconColor))
        {
            ImGui.TextUnformatted(iconString);
        }

        ImGui.SetCursorScreenPos(new Vector2(contentStartX + iconSize.X + innerGap, middleY - labelSize.Y * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, textColor))
        {
            ImGui.TextUnformatted(label);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);

        if (rawHovered && !string.IsNullOrEmpty(tooltip))
        {
            ImGui.SetTooltip(tooltip);
        }

        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                return true;
            }
        }

        return false;
    }
}
