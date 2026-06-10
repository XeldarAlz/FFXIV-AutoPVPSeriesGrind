using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class SettingsControls
{
    private const float DefaultSliderWidth = 200f;
    private const float DefaultComboWidth = 180f;
    private const float DelaySliderLabelOffset = 50f;
    private const float DelaySliderWidth = 220f;

    public static void DrawToggle(Configuration cfg, Func<bool> getter, Action<bool> setter)
    {
        var value = getter();
        if (ToggleSwitch.Draw(ref value))
        {
            setter(value);
            cfg.SaveDebounced();
        }
    }

    public static void DrawIntSlider(Configuration cfg, string id, Func<int> getter, Action<int> setter,
        int minimum, int maximum, string format = "%d", float width = DefaultSliderWidth)
    {
        var value = getter();
        ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.SliderGrab, Styling.AccentViolet)
            .Push(ImGuiCol.SliderGrabActive, Styling.AccentVioletSoft)
            .Push(ImGuiCol.FrameBg, Styling.CardBgSoft)
            .Push(ImGuiCol.FrameBgHovered, Styling.CardBgHover))
        {
            if (ImGui.SliderInt(id, ref value, minimum, maximum, format))
            {
                setter(Math.Clamp(value, minimum, maximum));
                cfg.SaveDebounced();
            }
        }
    }

    public static void DrawCombo(string id, string preview, string[] options, int selected, Action<int> onSelect,
        float width = DefaultComboWidth)
    {
        ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
        using var combo = ImRaii.Combo(id, preview);
        if (!combo)
        {
            return;
        }

        for (var optionIndex = 0; optionIndex < options.Length; optionIndex++)
        {
            if (ImGui.Selectable(options[optionIndex], optionIndex == selected))
            {
                onSelect(optionIndex);
            }
        }
    }

    public static void DrawLabeledDelaySlider(Configuration cfg, string id, string label,
        Func<int> getter, Action<int> setter, int maxSeconds)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
        {
            ImGui.TextUnformatted(label);
        }

        ImGui.SameLine(DelaySliderLabelOffset * ImGuiHelpers.GlobalScale);
        DrawIntSlider(cfg, id, getter, setter, 0, maxSeconds, "%d s", DelaySliderWidth);
    }

    public static void DrawDelayRange(Configuration cfg, string minId, string maxId,
        Func<int> getMin, Action<int> setMin, Func<int> getMax, Action<int> setMax, int maxSeconds)
    {
        DrawLabeledDelaySlider(cfg, minId, "Min", getMin, setMin, maxSeconds);
        DrawLabeledDelaySlider(cfg, maxId, "Max", getMax, setMax, maxSeconds);
    }
}
