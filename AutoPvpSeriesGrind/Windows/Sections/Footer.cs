using AutoPvpSeriesGrind.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class Footer
{
    public static void Draw()
    {
        ImGui.Separator();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted($"Auto PVP Series Grind — {ApsgConstants.PrimaryCommand} / {ApsgConstants.AliasCommand}");
    }
}
