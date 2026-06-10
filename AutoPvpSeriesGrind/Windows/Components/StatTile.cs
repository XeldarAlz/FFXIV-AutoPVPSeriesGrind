using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class StatTile
{
    public static void Draw(FontAwesomeIcon icon, string value, string caption, Vector4 accent, Vector2 size)
    {
        var s = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Vector4.Lerp(Styling.CardBg, accent, 0.10f)), Styling.CardRounding);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.BorderDim), Styling.CardRounding, ImDrawFlags.None, 1f);
        dl.AddRectFilled(origin, new Vector2(end.X, origin.Y + 3f * s), ImGui.GetColorU32(accent), Styling.CardRounding, ImDrawFlags.RoundCornersTop);

        var pad = 10f * s;
        var iconStr = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);

        var topY = origin.Y + 9f * s;
        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, topY));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, accent))
            ImGui.TextUnformatted(iconStr);

        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad + iconSize.X + 6f * s, topY + (iconSize.Y - ImGui.GetTextLineHeight()) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(caption);

        ImGui.SetWindowFontScale(1.45f);
        var valSize = ImGui.CalcTextSize(value);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, end.Y - valSize.Y - 8f * s));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(value);
        ImGui.SetWindowFontScale(1f);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }
}
