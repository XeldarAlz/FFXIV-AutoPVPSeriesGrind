using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed class PluginsPage
{
    private const float PadX = 16f;
    private const float DiscRadius = 17f;

    private const string Footer = "Install adds the plugin's own repository to Dalamud and queues an install. " +
        "If one-click install fails, right-click a plugin name to copy its repo URL and add it by hand under " +
        "/xlsettings, Experimental, Custom Plugin Repositories.";

    public void Draw()
    {
        var missing = 0;
        var plugins = ExternalPlugins.All;
        for (var index = 0; index < plugins.Count; index++)
        {
            if (ExternalPlugins.IsRequired(plugins[index]) && !ExternalPlugins.IsInstalled(plugins[index])) missing++;
        }

        var status = missing == 0
            ? "Everything the bot needs is installed and loaded."
            : missing == 1 ? "1 required plugin is missing." : $"{missing} required plugins are missing.";
        PageHeader.Draw("Plugins", status, missing == 0 ? Styling.AccentMint : Styling.AccentRose);

        for (var index = 0; index < plugins.Count; index++)
        {
            DrawCard(plugins[index]);
            Styling.VSpace(4f);
        }

        Styling.VSpace(6f);
        using (Fonts.PushCaption())
        {
            var origin = ImGui.GetCursorScreenPos();
            var width = ImGui.GetContentRegionAvail().X;
            TextDraw.Wrapped(Footer, origin, width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(Footer, width).Y));
        }
    }

    private static void DrawCard(ExternalPlugin plugin)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var info = ExternalPlugins.Catalog[plugin];
        var installed = ExternalPlugins.IsInstalled(plugin);
        var required = ExternalPlugins.IsRequired(plugin);
        var installing = PluginInstaller.IsInstalling(plugin);

        var (icon, accent) = (installed, required) switch
        {
            (true, _)      => (FontAwesomeIcon.CheckCircle, Styling.AccentMint),
            (false, true)  => (FontAwesomeIcon.TimesCircle, Styling.AccentRose),
            (false, false) => (FontAwesomeIcon.Circle, Styling.TextDim),
        };

        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.PluginCardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        Paint.Glass(dl, origin, end, Styling.CardRounding * scale, accent, 0.06f);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var discRadius = DiscRadius * scale;
        var discCenter = new Vector2(origin.X + padX + discRadius, midY);
        ProgressRing.Disc(discCenter, discRadius, Styling.Tint(Styling.Surface1, accent, 0.3f));
        ProgressRing.Track(discCenter, discRadius, 1.2f * scale, Styling.WithAlpha(accent, 0.7f));
        ProgressRing.CenterIcon(discCenter, icon, accent, discRadius * 0.95f);

        var rightWidth = DrawAction(plugin, installed, installing, end, midY);

        var textX = discCenter.X + discRadius + 16f * scale;
        var maxTextWidth = end.X - padX - rightWidth - textX;
        float nameHeight;
        using (Fonts.PushHeadline())
            nameHeight = TextDraw.Measure(info.DisplayName).Y;
        float purposeHeight;
        using (Fonts.PushCaption())
            purposeHeight = TextDraw.Measure(info.Purpose).Y;
        var top = midY - (nameHeight + 3f * scale + purposeHeight) * 0.5f;

        Vector2 nameSize;
        using (Fonts.PushHeadline())
        {
            nameSize = TextDraw.Measure(info.DisplayName);
            TextDraw.At(info.DisplayName, new Vector2(textX, top), Styling.TextStrong);
        }

        DrawRequirementTag(dl, required, textX + nameSize.X + 10f * scale, top + nameHeight * 0.5f);

        using (Fonts.PushCaption())
            TextDraw.At(TextDraw.Truncate(info.Purpose, maxTextWidth), new Vector2(textX, top + nameHeight + 3f * scale), Styling.TextDim);

        var nameMin = new Vector2(textX, top);
        var nameMax = nameMin + nameSize;
        if (Hit.HoveringRect(nameMin, nameMax))
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            Tooltip.Show($"Click to open {info.RepoUrl}\nRight-click to copy it.");
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) UrlActions.OpenOrCopy(info.RepoUrl);
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(info.RepoUrl);
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawRequirementTag(ImDrawListPtr dl, bool required, float x, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var label = TextDraw.Upper(required ? "Required" : "Optional");
        using (Fonts.PushCaption())
        {
            var labelSize = TextDraw.Measure(label);
            var tagMin = new Vector2(x, midY - labelSize.Y * 0.5f - 3f * scale);
            var tagMax = tagMin + labelSize + new Vector2(14f * scale, 6f * scale);
            var accent = required ? Styling.AccentViolet : Styling.TextDim;
            Paint.Pill(dl, tagMin, tagMax, Styling.WithAlpha(accent, 0.18f), Styling.WithAlpha(accent, 0.45f));
            TextDraw.Middle(label, tagMin, tagMax, required ? Styling.AccentVioletSoft : Styling.TextSecondary);
        }
    }

    private static float DrawAction(ExternalPlugin plugin, bool installed, bool installing, Vector2 end, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var padX = PadX * scale;

        if (installed)
        {
            const string label = "Installed";
            var labelSize = TextDraw.Measure(label);
            var iconSize = TextDraw.IconSize(FontAwesomeIcon.Check);
            var labelX = end.X - padX - labelSize.X;
            TextDraw.At(label, new Vector2(labelX, midY - labelSize.Y * 0.5f), Styling.AccentMint);
            var iconX = labelX - 6f * scale - iconSize.X;
            TextDraw.Icon(FontAwesomeIcon.Check, new Vector2(iconX, midY - iconSize.Y * 0.5f), Styling.AccentMint);
            return end.X - padX - iconX + 12f * scale;
        }

        var text = installing ? "Installing" : "Install";
        var width = PillButton.Width(text, FontAwesomeIcon.Download);
        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - width, midY - 15f * scale));
        ImGui.PushID((nint)((int)plugin + 1));
        if (PillButton.Draw("##install", text, Styling.AccentViolet, PillButton.Emphasis.Filled, FontAwesomeIcon.Download, enabled: !installing, height: 30f))
        {
            _ = PluginInstaller.Install(plugin);
        }

        ImGui.PopID();
        return width + 12f * scale;
    }
}
