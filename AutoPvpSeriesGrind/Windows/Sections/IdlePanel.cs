using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections;

// The idle (not-running) view: the activity hero grid, the one knob that matters (run-until goal), and the
// primary Start call-to-action. Mirrors the FATE plugin's idle layout.
internal static class IdlePanel
{
    public static void Draw(Configuration cfg, Plugin plugin, AutoPvpSeriesController ctrl)
    {
        GoalGrid.Draw(cfg, plugin);

        GoalSelector.Draw(cfg);
        ImGui.Spacing();

        var ready = ExternalPlugins.AllRequiredInstalled();
        if (PrimaryButton.Draw("START", Styling.AccentViolet, ready))
            ctrl.Start();
        if (!ready)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextWrapped("Install the required plugins first — open the dependencies window (the plug icon) to one-click them.");
    }
}
