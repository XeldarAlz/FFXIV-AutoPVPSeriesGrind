using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class IconButton
{
    public static bool Draw(FontAwesomeIcon icon, string id, string tooltip, Vector4? color, float size)
    {
        using var bg = ImRaii.PushColor(ImGuiCol.Button, Styling.CardBg)
            .Push(ImGuiCol.ButtonHovered, Styling.CardBgHover)
            .Push(ImGuiCol.ButtonActive, Styling.WithAlpha(Styling.AccentViolet, 0.55f))
            .Push(ImGuiCol.Border, Styling.BorderDim);
        using var border = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f);

        bool clicked;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? Styling.TextSecondary))
        {
            clicked = ImGui.Button(icon.ToIconString() + id, new Vector2(size, size));
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }

        return clicked;
    }
}
