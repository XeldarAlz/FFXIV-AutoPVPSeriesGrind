using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class Stepper
{
    private const float InputFieldWidth = 58f;
    private const float InnerSpacing = 4f;

    private static readonly Dictionary<string, (string MinusId, string InputId, string PlusId)> cachedIds = new();

    public static bool DrawCentered(string id, ref int value, int minimum, int maximum, int step, float scale)
    {
        var frameHeight = ImGui.GetFrameHeight();
        var inputWidth = InputFieldWidth * scale;
        var innerSpacing = InnerSpacing * scale;
        var width = frameHeight + innerSpacing + inputWidth + innerSpacing + frameHeight;
        Styling.CenterNextItem(width);

        var (minusId, inputId, plusId) = IdsFor(id);
        var buttonSize = new Vector2(frameHeight, frameHeight);

        var changed = false;
        using (ImRaii.PushColor(ImGuiCol.Button, Styling.CardBgSoft)
            .Push(ImGuiCol.ButtonHovered, Vector4.Lerp(Styling.CardBg, Styling.AccentViolet, 0.30f))
            .Push(ImGuiCol.ButtonActive, Styling.AccentViolet * 0.7f)
            .Push(ImGuiCol.Border, Styling.BorderDim))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(innerSpacing, ImGui.GetStyle().ItemSpacing.Y)))
        {
            if (DrawStepButton(FontAwesomeIcon.Minus, minusId, value <= minimum, buttonSize))
            {
                value = Math.Max(minimum, value - step);
                changed = true;
            }

            ImGui.SameLine();
            var inputValue = value;
            ImGui.SetNextItemWidth(inputWidth);
            if (ImGui.InputInt(inputId, ref inputValue, 0, 0))
            {
                value = Math.Clamp(inputValue, minimum, maximum);
                changed = true;
            }

            ImGui.SameLine();
            if (DrawStepButton(FontAwesomeIcon.Plus, plusId, value >= maximum, buttonSize))
            {
                value = Math.Min(maximum, value + step);
                changed = true;
            }
        }

        return changed;
    }

    private static bool DrawStepButton(FontAwesomeIcon icon, string buttonId, bool disabled, Vector2 buttonSize)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.Disabled(disabled))
        {
            return ImGui.Button(icon.ToIconString() + buttonId, buttonSize);
        }
    }

    private static (string MinusId, string InputId, string PlusId) IdsFor(string id)
    {
        if (!cachedIds.TryGetValue(id, out var ids))
        {
            ids = ($"##{id}_minus", $"##{id}", $"##{id}_plus");
            cachedIds[id] = ids;
        }

        return ids;
    }
}
