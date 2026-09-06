using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Shell;

internal static class ActionDock
{
    private const float PadX = 18f;

    public static void Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        Dock.Background(dl, origin, end, windowRounding);

        var padX = PadX * scale;
        var buttonHeight = Layout.HeroButtonHeight * scale;
        var innerWidth = size.X - padX * 2f;
        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, origin.Y + (size.Y - buttonHeight) * 0.5f));

        var ctrl = plugin.Controller;
        if (ctrl.Running) DrawStop(plugin, innerWidth);
        else DrawStart(plugin, innerWidth);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawStop(Plugin plugin, float innerWidth)
    {
        var ctrl = plugin.Controller;
        var session = ctrl.SessionSnapshot;
        var sub = session is null
            ? null
            : Loc.T(L.Shell.SessionSummary, session.MatchesCompleted, Formatting.Elapsed(session.Elapsed));
        if (StopButton.Draw(sub, innerWidth)) ctrl.Stop();
    }

    private static void DrawStart(Plugin plugin, float innerWidth)
    {
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;
        var depsOk = ExternalPlugins.AllRequiredInstalled();
        var reason = depsOk ? null : Loc.T(L.Shell.InstallRequired);
        var sub = Loc.T(L.Shell.ModeSummaryDot, ReadyState.StopSummary(cfg));

        if (StartButton.Draw(sub, depsOk, reason, innerWidth)) ctrl.Start();
    }
}
