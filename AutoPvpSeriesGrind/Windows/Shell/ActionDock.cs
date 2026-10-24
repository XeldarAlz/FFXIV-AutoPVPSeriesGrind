using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Shell;

internal static class ActionDock
{
    private const float PadX = 18f;
    private const float ButtonGap = 12f;

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
        if (ctrl.Running) DrawRunning(ctrl, innerWidth);
        else DrawStart(plugin, innerWidth);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawRunning(AutoPvpSeriesController ctrl, float innerWidth)
    {
        if (!ctrl.StopAfterMatchAvailable)
        {
            DrawStop(ctrl, innerWidth);
            return;
        }

        var gap = ButtonGap * ImGuiHelpers.GlobalScale;
        var half = (innerWidth - gap) * 0.5f;
        var origin = ImGui.GetCursorScreenPos();
        DrawStop(ctrl, half);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + half + gap, origin.Y));
        DrawFinish(ctrl, half);
    }

    private static void DrawStop(AutoPvpSeriesController ctrl, float width)
    {
        var session = ctrl.SessionSnapshot;
        var sub = session is null
            ? null
            : Loc.T(L.Shell.SessionSummary, session.MatchesCompleted, Formatting.Elapsed(session.Elapsed));
        if (StopButton.Draw(sub, width)) ctrl.Stop();
    }

    private static void DrawFinish(AutoPvpSeriesController ctrl, float width)
    {
        var title = Loc.T(ReadyState.FinishTitle(ctrl));
        var sub = Loc.T(ReadyState.FinishDetail(ctrl));
        if (HeroButton.Draw(FontAwesomeIcon.FlagCheckered, title, sub, ReadyState.FinishAccent(ctrl), true, null, width))
        {
            ctrl.ToggleStopAfterMatch();
        }
    }

    private static void DrawStart(Plugin plugin, float innerWidth)
    {
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;
        var depsOk = ExternalPlugins.AllRequiredInstalled();
        var conflict = PluginConflicts.Read();
        if (conflict.Blocked)
        {
            HeroButton.Draw(FontAwesomeIcon.Lock,
                Loc.T(conflict.PluginLoaded ? L.Conflicts.StartReason : L.Conflicts.CheckFailedTitle),
                null, Styling.AccentRose, false, width: innerWidth);
            return;
        }
        var blocker = depsOk ? ReadyState.ResolveStartBlocker() : StartBlocker.None;
        var enabled = depsOk && blocker == StartBlocker.None;
        var reason = !depsOk ? Loc.T(L.Shell.InstallRequired)
            : blocker != StartBlocker.None ? Loc.T(StartBlockerText.Title(blocker)) : null;
        var sub = Loc.T(L.Shell.ModeSummaryDot, Loc.T(ReadyState.ModeName(cfg)), ReadyState.StopSummary(cfg));

        if (StartButton.Draw(sub, enabled, reason, innerWidth)) ctrl.Start();
    }
}
