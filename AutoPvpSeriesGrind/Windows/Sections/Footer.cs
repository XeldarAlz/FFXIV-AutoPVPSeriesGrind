using AutoPvpSeriesGrind.Core;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class Footer
{
    private static readonly string CommandHint =
        $"Auto PVP Series Grind — {ApsgConstants.PrimaryCommand} / {ApsgConstants.AliasCommand}";

    public static void Draw()
    {
        ImGui.Separator();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.TextUnformatted(CommandHint);
        }
    }
}
