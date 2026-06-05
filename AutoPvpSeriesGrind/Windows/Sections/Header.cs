using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class Header
{
    public static void Draw(Plugin plugin, bool running)
    {
        var s = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();

        ImGui.AlignTextToFramePadding();
        var fh = ImGui.GetFrameHeight();
        var origin = ImGui.GetCursorScreenPos();
        var dotR = 3.5f * s;
        var dotCol = running
            ? Styling.PulseColor(Styling.AccentViolet, Styling.AccentVioletSoft, Styling.PulseCalm)
            : Styling.WithAlpha(Styling.AccentViolet, 0.5f);
        dl.AddCircleFilled(new Vector2(origin.X + dotR, origin.Y + fh * 0.5f), dotR, ImGui.GetColorU32(dotCol));

        ImGui.Dummy(new Vector2(dotR * 2 + 7 * s, fh));
        ImGui.SameLine(0, 0);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.SetWindowFontScale(1.0f);
            ImGui.TextUnformatted("AUTO PVP SERIES GRIND");
            ImGui.SetWindowFontScale(1f);
        }

        DrawIconCluster(plugin, fh);
        HairlineRule(s);
    }

    private static void DrawIconCluster(Plugin plugin, float btn)
    {
        var cfg = plugin.Configuration;
        var missing = !ExternalPlugins.AllRequiredInstalled();

        var actions = new List<(FontAwesomeIcon Icon, string Id, string Tip, Vector4? Color, Action OnClick)>();
        actions.Add((FontAwesomeIcon.ChartLine, "##apsg_history", "Run history", null, plugin.ToggleHistoryUi));
        actions.Add((FontAwesomeIcon.Plug, "##apsg_deps", missing ? "Required plugins missing" : "Dependencies",
            missing ? Styling.AccentRose : null, plugin.ToggleDependenciesUi));
        if (cfg.EnableCombatBrain)
            actions.Add((FontAwesomeIcon.Brain, "##apsg_brain", "Combat brain", BrainTint(), plugin.ToggleBrainUi));
        actions.Add((FontAwesomeIcon.InfoCircle, "##apsg_about", "About", null, plugin.ToggleAboutUi));
        actions.Add((FontAwesomeIcon.Cog, "##apsg_settings", "Settings", null, plugin.ToggleConfigUi));

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var total = actions.Count * btn + (actions.Count - 1) * spacing;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() - total);

        for (var i = 0; i < actions.Count; i++)
        {
            if (i > 0) ImGui.SameLine();
            var a = actions[i];
            if (IconBtn(a.Icon, a.Id, a.Tip, a.Color, btn))
                a.OnClick();
        }
    }

    // Tints the brain icon to the live decision colour while telemetry is fresh — a quiet cue on the
    // main window that the brain is actively thinking, without surfacing the full readout here.
    private static Vector4? BrainTint()
    {
        if (BrainTelemetry.IsFresh && BrainTelemetry.Plan is { } p)
            return p.Kind switch
            {
                MoveKind.Retreat => Styling.AccentRose,
                MoveKind.Engage => Styling.AccentAmber,
                _ => Styling.AccentMint,
            };
        return null;
    }

    private static bool IconBtn(FontAwesomeIcon icon, string id, string tooltip, Vector4? color, float size)
    {
        using var bg = ImRaii.PushColor(ImGuiCol.Button, Styling.CardBg)
            .Push(ImGuiCol.ButtonHovered, Styling.CardBgHover)
            .Push(ImGuiCol.ButtonActive, Styling.WithAlpha(Styling.AccentViolet, 0.55f))
            .Push(ImGuiCol.Border, Styling.BorderDim);
        using var border = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f);

        bool clicked;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? Styling.TextSecondary))
            clicked = ImGui.Button(icon.ToIconString() + id, new Vector2(size, size));

        if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        return clicked;
    }

    private static void HairlineRule(float s)
    {
        Styling.VSpace(2f);
        var dl = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();
        var w = ImGui.GetContentRegionAvail().X;
        dl.AddLine(p, p + new Vector2(w, 0), ImGui.GetColorU32(Styling.Hairline), 1f);
        ImGui.Dummy(new Vector2(w, 1f));
        Styling.VSpace(2f);
    }
}
