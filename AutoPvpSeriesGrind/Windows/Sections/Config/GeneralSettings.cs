using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

// "Session" tab — how a run starts and when it stops.
internal static class GeneralSettings
{
    public static void Draw(Configuration cfg)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextWrapped("The stop condition (matches / Series rank / time / endless) lives on the main window under \"Run until\".");
        ImGui.Spacing();
        ImGui.Spacing();

        SettingsRow.Draw("Gearset slot", "Equip this gear set before the first queue, so you always start on the right PvP job. The number matches the gear set list in-game. 0 leaves your current gear alone.",
            () =>
            {
                var v = cfg.GearsetSlot;
                ImGui.SetNextItemWidth(140f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputInt("##gearset", ref v))
                {
                    cfg.GearsetSlot = Math.Clamp(v, 0, 100);
                    cfg.SaveDebounced();
                }
            });

        SettingsRow.Draw("Open this window on login", "Show the main window automatically each time you log in.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.AutoShowOnLogin, v => cfg.AutoShowOnLogin = v, "##autoshow"));
    }
}
