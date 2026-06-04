using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
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

        SettingsRow.Draw("Open this window on login", "Show the main window automatically each time you log in.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.AutoShowOnLogin, v => cfg.AutoShowOnLogin = v, "##autoshow"));
    }
}
