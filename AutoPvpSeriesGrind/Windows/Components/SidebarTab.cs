using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class SidebarTab
{
    private const float TabHeight = 40f;
    private const float CornerRounding = 6f;
    private const float SelectionBarWidth = 3f;
    private const float SelectionBarRounding = 1f;
    private const float ContentPaddingX = 14f;
    private const float IconLabelGap = 10f;

    public static bool Draw(string label, FontAwesomeIcon icon, Vector4 accent, bool selected)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var height = TabHeight * globalScale;
        var width = ImGui.GetContentRegionAvail().X;

        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(width, height);
        var drawList = ImGui.GetWindowDrawList();
        var hovered = ImGui.IsMouseHoveringRect(origin, end);

        var background = selected
            ? Vector4.Lerp(Styling.CardBg, accent, 0.18f)
            : hovered ? Styling.CardBgHover : new Vector4(0, 0, 0, 0);

        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(background), CornerRounding);
        if (selected)
        {
            drawList.AddRectFilled(origin, new Vector2(origin.X + SelectionBarWidth * globalScale, end.Y),
                ImGui.GetColorU32(accent), SelectionBarRounding);
        }

        var paddingX = ContentPaddingX * globalScale;
        var iconString = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            iconSize = ImGui.CalcTextSize(iconString);
        }

        var iconPos = new Vector2(origin.X + paddingX, origin.Y + (height - iconSize.Y) * 0.5f);
        ImGui.SetCursorScreenPos(iconPos);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, selected ? accent : Styling.TextSecondary))
        {
            ImGui.TextUnformatted(iconString);
        }

        var labelSize = ImGui.CalcTextSize(label);
        var labelPos = new Vector2(
            origin.X + paddingX + iconSize.X + IconLabelGap * globalScale,
            origin.Y + (height - labelSize.Y) * 0.5f);
        ImGui.SetCursorScreenPos(labelPos);
        using (ImRaii.PushColor(ImGuiCol.Text, selected ? Styling.TextStrong : Styling.TextSecondary))
        {
            ImGui.TextUnformatted(label);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));

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
