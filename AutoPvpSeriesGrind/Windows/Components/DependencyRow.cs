using AutoPvpSeriesGrind.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class DependencyRow
{
    public static void Draw(ExternalPlugin plugin)
    {
        var info = ExternalPlugins.Catalog[plugin];
        var installed = ExternalPlugins.IsInstalled(plugin);
        var disabled = ExternalPlugins.IsInstalledButDisabled(plugin);
        var installing = PluginInstaller.IsInstalling(plugin);

        ImGui.TableNextRow();

        ImGui.TableSetColumnIndex(0);
        DrawStatusIcon(installed, disabled, info.Required);

        ImGui.TableSetColumnIndex(1);
        DrawName(info);

        ImGui.TableSetColumnIndex(2);
        DrawAction(plugin, info, installed, disabled, installing);
    }

    private static void DrawStatusIcon(bool installed, bool disabled, bool required)
    {
        var (icon, color) = (installed, disabled, required) switch
        {
            (true,  true,  _    ) => (FontAwesomeIcon.ExclamationCircle, Styling.AccentAmber),
            (true,  false, _    ) => (FontAwesomeIcon.CheckCircle,       Styling.AccentMint),
            (false, _,     true ) => (FontAwesomeIcon.TimesCircle,       Styling.AccentRose),
            (false, _,     false) => (FontAwesomeIcon.Circle,            Styling.TextDim),
        };
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
            ImGui.TextUnformatted(icon.ToIconString());
    }

    private static void DrawName(ExternalPluginInfo info)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(info.DisplayName);
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted(info.Required ? "  required" : "  optional");

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            using (ImRaii.Tooltip())
                ImGui.TextUnformatted($"Repo: {info.RepoUrl}\nLeft-click to open repo URL · right-click to copy");
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) UrlActions.OpenInBrowser(info.RepoUrl);
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(info.RepoUrl);
        }
    }

    private static void DrawAction(ExternalPlugin plugin, ExternalPluginInfo info, bool installed, bool disabled, bool installing)
    {
        var size = new Vector2(110 * ImGuiHelpers.GlobalScale, 0);
        if (installed)
        {
            var (text, color) = disabled ? ("disabled", Styling.AccentAmber) : ("installed", Styling.AccentMint);
            using (ImRaii.PushColor(ImGuiCol.Text, color))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(text);
            }
            if (disabled && ImGui.IsItemHovered())
                using (ImRaii.Tooltip())
                    ImGui.TextUnformatted("Loaded, but the plugin's own \"Enable\" toggle is off.");
            return;
        }

        var failed = !installing && PluginInstaller.DidFail(plugin);

        var (btnColor, hoverColor, activeColor) = failed
            ? (Styling.AccentRose * 0.55f, Styling.AccentRose * 0.75f, Styling.AccentRose)
            : (Styling.AccentTeal * 0.55f, Styling.AccentTeal * 0.75f, Styling.AccentTeal);

        using (ImRaii.Disabled(installing))
        using (ImRaii.PushColor(ImGuiCol.Button, btnColor))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, hoverColor))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, activeColor))
        {
            var label = installing ? "Installing..." : failed ? "Retry" : "Install";
            if (ImGui.Button($"{label}##install_{plugin}", size))
                _ = PluginInstaller.Install(plugin);
        }

        if (failed)
        {
            if (ImGui.IsItemHovered())
                using (ImRaii.Tooltip())
                    ImGui.TextUnformatted(
                        "Automatic install failed (see /xllog for details).\n"
                        + "Add the repo manually in Dalamud → Settings → Experimental →\n"
                        + "Custom Plugin Repositories, then install from the plugin list:\n"
                        + info.RepoUrl);

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                ImGui.TextUnformatted(FontAwesomeIcon.ExclamationTriangle.ToIconString());
            if (ImGui.IsItemHovered())
                using (ImRaii.Tooltip())
                    ImGui.TextUnformatted($"Install failed — left-click to copy repo URL:\n{info.RepoUrl}");
            else if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                ImGui.SetClipboardText(info.RepoUrl);
        }
    }
}
