using AutoPvpSeriesGrind.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;
using System.Text;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class DependencyBanner
{
    public static void Draw(Plugin plugin)
    {
        var missingNames = BuildMissingRequiredNames();
        if (missingNames == null)
        {
            return;
        }

        DrawBanner(plugin, $"Missing required: {missingNames}");
    }

    private static string? BuildMissingRequiredNames()
    {
        StringBuilder? names = null;
        foreach (var externalPlugin in ExternalPlugins.All)
        {
            var info = ExternalPlugins.Catalog[externalPlugin];
            if (!info.Required || ExternalPlugins.IsInstalled(externalPlugin))
            {
                continue;
            }

            if (names == null)
            {
                names = new StringBuilder(info.DisplayName);
            }
            else
            {
                names.Append(", ").Append(info.DisplayName);
            }
        }

        return names?.ToString();
    }

    private static void DrawBanner(Plugin plugin, string message)
    {
        Vector4 bannerColor = Styling.AccentRose;
        using (ImRaii.PushColor(ImGuiCol.Border, bannerColor))
        using (ImRaii.PushColor(ImGuiCol.ChildBg, Styling.CardBgSoft))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1.5f))
        using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6f))
        using (ImRaii.Child("##depbanner", new(-1, 46), true))
        {
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.PushColor(ImGuiCol.Text, bannerColor))
            {
                ImGui.TextUnformatted(FontAwesomeIcon.ExclamationTriangle.ToIconString());
            }

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, bannerColor))
            {
                ImGui.TextUnformatted(message);
            }

            const string label = "Manage";
            var buttonWidth = ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2 + 4f;
            ImGui.SameLine(ImGui.GetContentRegionMax().X - buttonWidth);
            if (ImGui.Button(label))
            {
                plugin.ToggleDependenciesUi();
            }
        }
    }
}
