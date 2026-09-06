using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Sections;
using AutoPvpSeriesGrind.Windows.Shell;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed class GrindPage
{
    private const float SwitchRevealMs = 320f;
    private const int PayoffMs = 8000;
    private const float PayoffRingRadius = 58f;
    private const float PayoffRingThickness = 7f;

    public void Draw(Plugin plugin, AppWindow window)
    {
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;

        if (DrawPayoff(ctrl)) return;

        var running = ctrl.Running;
        using var reveal = Motion.PushSwitch("##apsg_grind_state", running, SwitchRevealMs);
        if (running) RunningPanel.Draw(cfg, ctrl);
        else DrawIdle(plugin, window, cfg, ctrl);
    }

    private static void DrawIdle(Plugin plugin, AppWindow window, Configuration cfg, AutoPvpSeriesController ctrl)
    {
        if (Headline.Draw(cfg, ctrl, plugin.History)) window.Show(AppWindow.Page.Plugins);
        Styling.VSpace(20f);

        PlanCard.Draw(cfg, ctrl);
        Styling.VSpace(12f);
    }

    private static bool DrawPayoff(AutoPvpSeriesController ctrl)
    {
        if (!ctrl.LastByGoal || !ctrl.HasRecentResult(PayoffMs)) return false;

        var scale = ImGuiHelpers.GlobalScale;

        Styling.VSpace(30f);
        using (Fonts.PushCaption())
            Styling.TextCentered(TextDraw.Upper(Loc.T(L.Common.Done)), Styling.AccentAmberSoft);
        Styling.VSpace(16f);

        DrawPayoffRing(scale);
        Styling.VSpace(18f);

        using (Fonts.PushTitle())
            Styling.TextCentered(Loc.T(L.Grind.SessionMatches, ctrl.LastMatches), Styling.TextStrong);
        Styling.VSpace(8f);

        var detail = ctrl.LastSeriesExp > 0
            ? Loc.T(L.Grind.SessionExp, Formatting.Exp(ctrl.LastSeriesExp))
            : Loc.T(L.Grind.SessionComplete);
        Styling.TextCentered(detail, Styling.TextSecondary);
        Styling.VSpace(20f);

        var label = Loc.T(L.Grind.BackToPlan);
        var width = PillButton.Width(label, FontAwesomeIcon.ArrowRight);
        Styling.CenterNextItem(width);
        if (PillButton.Draw("##apsg_payoff_dismiss", label, Styling.AccentViolet, PillButton.Emphasis.Tinted, FontAwesomeIcon.ArrowRight))
        {
            ctrl.ClearLastResult();
        }

        return true;
    }

    private static void DrawPayoffRing(float scale)
    {
        var radius = PayoffRingRadius * scale;
        var thickness = PayoffRingThickness * scale;

        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(origin.X + avail * 0.5f, origin.Y + radius + 4f * scale);

        ProgressRing.Glow(center, radius, Styling.AccentAmber, 0.6f + 0.4f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.85f));
        ProgressRing.Fill(center, radius, thickness, 1f, Styling.AccentAmber);
        ProgressRing.Sweep(center, radius, thickness * 0.55f, Styling.AccentAmberSoft, Styling.PulseOrbit, 1.0f, 0.40f);
        ProgressRing.CenterIcon(center, FontAwesomeIcon.Check, Styling.AccentAmberSoft, radius * 0.62f);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(avail, radius * 2f + 8f * scale));
    }
}
