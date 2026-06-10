using AutoPvpSeriesGrind.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class DependencyRow
{
    private const float ButtonRestingShade = 0.55f;
    private const float ButtonHoverShade = 0.75f;

    private static readonly Dictionary<ExternalPlugin, string> installButtonIdSuffixes = BuildInstallButtonIdSuffixes();

    public static void Draw(ExternalPlugin plugin)
    {
        var info = ExternalPlugins.Catalog[plugin];
        var installed = ExternalPlugins.IsInstalled(plugin);
        var installing = PluginInstaller.IsInstalling(plugin);
        var required = ExternalPlugins.IsRequired(plugin);

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        DrawStatusIcon(installed, required);

        ImGui.TableSetColumnIndex(1);
        DrawName(info, required);

        ImGui.TableSetColumnIndex(2);
        DrawAction(plugin, info, installed, installing);
    }

    private static void DrawStatusIcon(bool installed, bool required)
    {
        var (icon, color) = (installed, required) switch
        {
            (true,  _    ) => (FontAwesomeIcon.CheckCircle, Styling.AccentMint),
            (false, true ) => (FontAwesomeIcon.TimesCircle, Styling.AccentRose),
            (false, false) => (FontAwesomeIcon.Circle,      Styling.TextDim),
        };
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(icon.ToIconString());
    }

    private static void DrawName(ExternalPluginInfo info, bool required)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(info.DisplayName);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted(required ? "  required" : "  optional");

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using (ImRaii.Tooltip())
                ImGui.TextUnformatted($"Repo: {info.RepoUrl}\nLeft-click to open repo URL · right-click to copy");
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) UrlActions.OpenInBrowser(info.RepoUrl);
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(info.RepoUrl);
        }
    }

    private static void DrawAction(ExternalPlugin plugin, ExternalPluginInfo info, bool installed, bool installing)
    {
        if (installed)
        {
            DrawInstalledLabel();
            return;
        }

        var failed = !installing && PluginInstaller.DidFail(plugin);
        DrawInstallButton(plugin, installing, failed);
        if (failed)
        {
            DrawFailureBadge(info);
        }
    }

    private static void DrawInstalledLabel()
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentMint))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("installed");
        }
    }

    private static void DrawInstallButton(ExternalPlugin plugin, bool installing, bool failed)
    {
        var size = new Vector2(110 * ImGuiHelpers.GlobalScale, 0);
        var (buttonColor, hoverColor, activeColor) = failed
            ? (Styling.AccentRose * ButtonRestingShade, Styling.AccentRose * ButtonHoverShade, Styling.AccentRose)
            : (Styling.AccentViolet * ButtonRestingShade, Styling.AccentViolet * ButtonHoverShade, Styling.AccentViolet);

        using (ImRaii.Disabled(installing))
        using (ImRaii.PushColor(ImGuiCol.Button, buttonColor))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, hoverColor))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, activeColor))
        {
            var label = installing ? "Installing..." : failed ? "Retry" : "Install";
            if (ImGui.Button(label + installButtonIdSuffixes[plugin], size))
            {
                _ = PluginInstaller.Install(plugin);
            }
        }
    }

    private static void DrawFailureBadge(ExternalPluginInfo info)
    {
        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                ImGui.TextUnformatted(
                    "Automatic install failed (see /xllog for details).\n"
                    + "Add the repo manually in Dalamud → Settings → Experimental →\n"
                    + "Custom Plugin Repositories, then install from the plugin list:\n"
                    + info.RepoUrl);
            }
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
        {
            ImGui.TextUnformatted(FontAwesomeIcon.ExclamationTriangle.ToIconString());
        }

        if (ImGui.IsItemHovered())
            using (ImRaii.Tooltip())
                ImGui.TextUnformatted($"Install failed — left-click to copy repo URL:\n{info.RepoUrl}");
        else if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ImGui.SetClipboardText(info.RepoUrl);
    }

    private static Dictionary<ExternalPlugin, string> BuildInstallButtonIdSuffixes()
    {
        var pluginValues = Enum.GetValues<ExternalPlugin>();
        var suffixes = new Dictionary<ExternalPlugin, string>(pluginValues.Length);
        for (var pluginIndex = 0; pluginIndex < pluginValues.Length; pluginIndex++)
        {
            var plugin = pluginValues[pluginIndex];
            suffixes[plugin] = $"##install_{plugin}";
        }

        return suffixes;
    }
}
