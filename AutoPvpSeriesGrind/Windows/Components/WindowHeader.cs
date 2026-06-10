using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class WindowHeader
{
    public static void Draw(string title, string subtitle, float fontScale)
    {
        ImGui.SetWindowFontScale(fontScale);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(title);
        ImGui.SetWindowFontScale(1.0f);

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted(subtitle);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
}
