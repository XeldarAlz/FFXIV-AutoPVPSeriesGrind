using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class PrimaryButton
{
    private const float RestingShade = 0.65f;
    private const float HoverShade = 0.85f;
    private const float CornerRounding = 8f;

    public static bool Draw(string label, Vector4 accent, bool enabled = true, float width = -1)
    {
        var height = Layout.PrimaryButtonHeight * ImGuiHelpers.GlobalScale;
        using var disabledScope = ImRaii.Disabled(!enabled);
        using var colorScope = ImRaii.PushColor(ImGuiCol.Button, enabled ? accent * RestingShade : Styling.CardBgSoft)
            .Push(ImGuiCol.ButtonHovered, enabled ? accent * HoverShade : Styling.CardBgSoft)
            .Push(ImGuiCol.ButtonActive, enabled ? accent : Styling.CardBgSoft)
            .Push(ImGuiCol.Text, enabled ? Styling.TextStrong : Styling.TextMuted);
        using var roundingScope = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, CornerRounding);

        return ImGui.Button(label, new Vector2(width, height));
    }
}
