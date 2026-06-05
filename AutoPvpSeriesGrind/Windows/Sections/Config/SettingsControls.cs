using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class SettingsControls
{
    public static void DrawToggle(Configuration cfg, Func<bool> getter, Action<bool> setter, string id)
    {
        var v = getter();
        if (ToggleSwitch.Draw(id, ref v))
        {
            setter(v);
            cfg.SaveDebounced();
        }
    }

    public static void DrawIntSlider(Configuration cfg, string id, Func<int> getter, Action<int> setter,
        int min, int max, string format = "%d", float width = 200f)
    {
        var v = getter();
        ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.SliderGrab, Styling.AccentViolet))
        using (ImRaii.PushColor(ImGuiCol.SliderGrabActive, Styling.AccentVioletSoft))
        using (ImRaii.PushColor(ImGuiCol.FrameBg, Styling.CardBgSoft))
        using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, Styling.CardBgHover))
            if (ImGui.SliderInt(id, ref v, min, max, format))
            {
                setter(Math.Clamp(v, min, max));
                cfg.SaveDebounced();
            }
    }

    public static void DrawCombo(string id, string preview, string[] options, int selected, Action<int> onSelect,
        float width = 180f)
    {
        ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
        using var combo = ImRaii.Combo(id, preview);
        if (!combo) return;
        for (var i = 0; i < options.Length; i++)
            if (ImGui.Selectable(options[i], i == selected))
                onSelect(i);
    }
}
