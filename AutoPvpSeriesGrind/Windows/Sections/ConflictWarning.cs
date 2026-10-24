using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using System.IO;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class ConflictWarning
{
    private const long DisableWaitMs = 8000;
    private static long? disableRequestedAtMs;
    private static bool disableFailed;
    private static string? iconPath;

    public static bool Draw()
    {
        var state = PluginConflicts.Read();
        if (!state.PluginLoaded)
        {
            disableRequestedAtMs = null;
            disableFailed = false;
        }
        if (!state.Blocked) return false;

        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var padding = 20f * scale;
        var innerWidth = Math.Max(1f, width - padding * 2f);
        var left = origin.X + padding;
        var y = origin.Y + padding;
        var dl = ImGui.GetWindowDrawList();
        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1);

        var iconSize = 64f * scale;
        DrawIcon(dl, new Vector2(left, y), iconSize, state.PluginLoaded);
        var titleX = left + iconSize + 16f * scale;
        var titleWidth = Math.Max(1f, origin.X + width - padding - titleX);
        var title = Loc.T(state.PluginLoaded ? L.Conflicts.Title : L.Conflicts.CheckFailedTitle);
        float titleHeight;
        using (Fonts.PushHeadline())
        {
            titleHeight = TextDraw.MeasureWrapped(title, titleWidth).Y;
            TextDraw.Wrapped(title, new Vector2(titleX, y), titleWidth, Styling.AccentRoseSoft);
        }
        using (Fonts.PushCaption())
        {
            var badgeY = y + titleHeight + 8f * scale;
            var label = Loc.T(L.Conflicts.Blocked);
            TextDraw.Wrapped(label, new Vector2(titleX, badgeY), titleWidth, Styling.AccentRose);
            y += Math.Max(iconSize, titleHeight + 8f * scale + TextDraw.MeasureWrapped(label, titleWidth).Y) + 20f * scale;
        }

        if (state.PluginLoaded) Paragraph(Loc.T(L.Conflicts.Detail), Styling.TextStrong);
        if (state.DetectionFailed) Paragraph(Loc.T(L.Conflicts.CheckFailed), Styling.AccentRoseSoft);

        var pending = disableRequestedAtMs is { } requested && Environment.TickCount64 - requested < DisableWaitMs;
        if (state.PluginLoaded && (disableRequestedAtMs.HasValue || disableFailed))
            Paragraph(Loc.T(pending ? L.Conflicts.DisablePending : L.Conflicts.DisableFailed), Styling.AccentRoseSoft);

        var buttonX = left;
        if (state.PluginLoaded)
        {
            if (Button("##disable_rsr", Loc.T(pending ? L.Conflicts.Disabling : L.Conflicts.Disable), FontAwesomeIcon.PowerOff, !pending))
            {
                disableFailed = !PluginConflicts.DisableRotationSolver();
                disableRequestedAtMs = disableFailed ? null : Environment.TickCount64;
            }
        }
        if (Button("##rsr_plugins", Loc.T(L.Conflicts.OpenPlugins), FontAwesomeIcon.PuzzlePiece))
            PluginConflicts.OpenPluginInstaller();
        var end = new Vector2(origin.X + width, y + 32f * scale + padding);
        dl.ChannelsSetCurrent(0);
        Paint.Surface(dl, origin, end, Styling.CardRounding * scale,
            Styling.Tint(Styling.Surface0, Styling.AccentRose, 0.12f), Styling.WithAlpha(Styling.AccentRose, 0.85f),
            borderThickness: 2f * scale);
        dl.ChannelsMerge();
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(end - origin);
        return true;

        void Paragraph(string text, Vector4 color)
        {
            TextDraw.Wrapped(text, new Vector2(left, y), innerWidth, color);
            y += TextDraw.MeasureWrapped(text, innerWidth).Y + 14f * scale;
        }

        bool Button(string id, string label, FontAwesomeIcon icon, bool enabled = true)
        {
            var buttonWidth = PillButton.Width(label, icon);
            if (buttonX > left && buttonX + buttonWidth > left + innerWidth)
            {
                buttonX = left;
                y += 42f * scale;
            }
            ImGui.SetCursorScreenPos(new Vector2(buttonX, y));
            var clicked = PillButton.Draw(id, label, Styling.AccentRose, PillButton.Emphasis.Tinted, icon, enabled, height: 32f);
            buttonX += buttonWidth + 10f * scale;
            return clicked;
        }
    }

    private static void DrawIcon(ImDrawListPtr dl, Vector2 origin, float size, bool detected)
    {
        iconPath ??= Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty, "Images", "RotationSolverReborn.png");
        if (detected && File.Exists(iconPath)
            && Svc.Texture.GetFromFile(iconPath).TryGetWrap(out var texture, out _))
        {
            dl.AddImageRounded(texture.Handle, origin, origin + new Vector2(size), Vector2.Zero, Vector2.One,
                Paint.Col(Vector4.One), 8f * ImGuiHelpers.GlobalScale);
            return;
        }
        TextDraw.IconCentered(FontAwesomeIcon.ExclamationTriangle, origin + new Vector2(size * 0.5f), Styling.AccentRose, 2f);
    }
}
