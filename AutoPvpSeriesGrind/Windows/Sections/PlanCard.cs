using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Modes;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class PlanCard
{
    private const string GoalPopup = "##session_goal_editor";
    private const string AfterPopup = "##session_after_editor";
    private const float ButtonHeight = 34f;
    private const float PopupWidth = 480f;

    private static readonly Segmented.Item[] Goals = new Segmented.Item[4];
    private static readonly Segmented.Item[] After = new Segmented.Item[4];
    private static readonly AfterRunAction[] Actions =
        [AfterRunAction.StayLoggedIn, AfterRunAction.ReturnToInn, AfterRunAction.Logout, AfterRunAction.CloseGame];
    private static readonly (LocString Label, LocString Help, FontAwesomeIcon Icon)[] AfterChoices =
    [
        (L.Plan.AfterStayLabel, L.Plan.AfterStayHelp, FontAwesomeIcon.MapMarkerAlt),
        (L.Plan.AfterInnLabel, L.Plan.AfterInnHelp, FontAwesomeIcon.Bed),
        (L.Plan.AfterLogoutLabel, L.Plan.AfterLogoutHelp, FontAwesomeIcon.SignOutAlt),
        (L.Plan.AfterCloseLabel, L.Plan.AfterCloseHelp, FontAwesomeIcon.PowerOff),
    ];

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var pad = new Vector2(12f, 8f) * scale;
        var gap = 8f * scale;
        var height = MathF.Max(ButtonHeight * scale, ImGui.GetTextLineHeight() + 12f * scale);
        var title = Loc.T(L.Plan.Title);
        var titleSize = TextDraw.Measure(title);
        var fieldX = origin.X + pad.X + titleSize.X + 12f * scale;
        var right = origin.X + width - pad.X;
        var available = MathF.Max(1f, right - fieldX);
        var endless = cfg.ActiveMode.Id == EndlessMode.ModeId;
        var afterIndex = Math.Max(0, Array.IndexOf(Actions, cfg.AfterRun));
        var goal = GoalLabel(cfg);
        var after = Loc.T(endless ? L.Setup.AfterInactive : AfterChoices[afterIndex].Label);
        var afterIcon = endless ? FontAwesomeIcon.Minus : AfterChoices[afterIndex].Icon;
        var goalLabel = FitButtonLabel(goal, FontAwesomeIcon.Flag, available);
        var afterLabel = FitButtonLabel(after, afterIcon, available);
        var goalWidth = PillButton.Width(goalLabel, FontAwesomeIcon.Flag);
        var afterWidth = PillButton.Width(afterLabel, afterIcon);
        var arrowWidth = TextDraw.IconSize(FontAwesomeIcon.ArrowRight).X;
        var sameRow = goalWidth + gap * 2f + arrowWidth + afterWidth <= available;
        var size = new Vector2(width, pad.Y * 2f + height * (sameRow ? 1f : 2f) + (sameRow ? 0f : gap));
        var dl = ImGui.GetWindowDrawList();
        Paint.Surface(dl, origin, origin + size, Styling.PanelRounding * scale,
            Styling.WithAlpha(Styling.Surface1, 0.5f), Styling.WithAlpha(Styling.BorderDim, 0.5f));
        var firstY = origin.Y + pad.Y;
        var titlePos = new Vector2(origin.X + pad.X, firstY + (height - titleSize.Y) * 0.5f);
        TextDraw.At(title, titlePos, Styling.TextDim);

        ImGui.SetCursorScreenPos(new Vector2(fieldX, firstY));
        if (PillButton.Draw("##goal_summary", goalLabel, Styling.AccentArc, icon: FontAwesomeIcon.Flag,
                enabled: !ctrl.Running, height: height / scale, tooltip: Loc.T(L.Setup.RunGoal) + "\n" + goal))
            ImGui.OpenPopup(GoalPopup);

        var afterPos = sameRow ? new Vector2(fieldX + goalWidth + gap * 2f + arrowWidth, firstY)
            : new Vector2(fieldX, firstY + height + gap);
        TextDraw.IconCentered(sameRow ? FontAwesomeIcon.ArrowRight : FontAwesomeIcon.LevelDownAlt,
            sameRow ? new Vector2(fieldX + goalWidth + gap + arrowWidth * 0.5f, firstY + height * 0.5f)
                : new Vector2(origin.X + pad.X + titleSize.X * 0.5f, afterPos.Y + height * 0.5f), Styling.TextDim);
        ImGui.SetCursorScreenPos(afterPos);
        var afterEnabled = !ctrl.Running && !endless;
        var afterHelp = Loc.T(endless ? L.Setup.AfterEndless : AfterChoices[afterIndex].Help);
        if (PillButton.Draw("##after_summary", afterLabel, Styling.AccentArc, PillButton.Emphasis.Ghost,
                afterIcon, enabled: afterEnabled, height: height / scale,
                tooltip: Loc.T(L.Plan.AfterGoalTitle) + "\n" + afterHelp))
            ImGui.OpenPopup(AfterPopup);
        if (!afterEnabled && Hit.HoveringRect(afterPos, afterPos + new Vector2(afterWidth, height)))
            Tooltip.Show(ctrl.Running ? Loc.T(L.Plan.Locked) : afterHelp);

        var summary = Loc.T(L.Plan.SeriesRank, PvpProfileReader.SeriesCurrentRank());
        if (cfg.TakeBreaks) summary += TextDraw.Separator + Loc.T(L.Plan.BreakEveryHelp, cfg.BreakMinutes, cfg.BreakEveryMatches);
        if (Hit.HoveringRect(titlePos, titlePos + titleSize)) Tooltip.Show(summary);
        var metaX = afterPos.X + afterWidth + 18f * scale;
        if (right - metaX >= 100f * scale)
        {
            using (Fonts.PushCaption())
            {
                var caption = Loc.T(L.Plan.SeriesRank, PvpProfileReader.SeriesCurrentRank());
                if (cfg.TakeBreaks) caption += TextDraw.Separator + Loc.T(L.Plan.BreakEvery, cfg.BreakEveryMatches);
                var metaPos = new Vector2(metaX, afterPos.Y + (height - ImGui.GetTextLineHeight()) * 0.5f);
                TextDraw.At(TextDraw.Truncate(caption, right - metaX), metaPos, Styling.TextDim);
                if (Hit.HoveringRect(metaPos, new Vector2(right, afterPos.Y + height))) Tooltip.Show(summary);
            }
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
        DrawEditor(GoalPopup, cfg, ctrl.Running, goal: true);
        DrawEditor(AfterPopup, cfg, ctrl.Running, goal: false);
    }

    private static string FitButtonLabel(string label, FontAwesomeIcon icon, float width)
        => TextDraw.Truncate(label, MathF.Max(1f, width - PillButton.Width(string.Empty, icon)));

    private static string GoalLabel(Configuration cfg) => cfg.ActiveMode.Id switch
    {
        MatchCountMode.ModeId => Loc.T(L.Plan.GoalMatches, cfg.TargetMatchCount),
        SeriesRankMode.ModeId => Loc.T(L.Plan.GoalRank,
            Math.Max(cfg.TargetSeriesRank, SeriesRankMode.LowestOpenTarget(PvpProfileReader.SeriesCurrentRank()))),
        TimeBoxedMode.ModeId => Loc.T(L.Plan.GoalMinutes, cfg.TargetMinutes),
        _ => Loc.T(L.Plan.TabEndless),
    };

    private static void DrawEditor(string id, Configuration cfg, bool running, bool goal)
    {
        if (!ImGui.IsPopupOpen(id)) return;
        var scale = ImGuiHelpers.GlobalScale;
        var viewport = ImGui.GetMainViewport();
        var width = MathF.Min(PopupWidth * scale, viewport.WorkSize.X - 24f * scale);
        // ImGui places the popup within the viewport; constrain its size so long translations
        // wrap and small displays can scroll without growing the closed run-plan strip.
        ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0f),
            new Vector2(width, MathF.Max(1f, viewport.WorkSize.Y - 24f * scale)));
        using var popup = ContextMenu.Begin(id);
        if (!popup.Open) return;
        using var disabled = ImRaii.Disabled(running);
        if (goal)
        {
            DrawGoal(cfg);
            Styling.VSpace(6f);
            if (PillButton.Draw("##goal_done", Loc.T(L.Common.Done), Styling.AccentArc, icon: FontAwesomeIcon.Check))
                ImGui.CloseCurrentPopup();
        }
        else DrawAfter(cfg);
    }

    private static void DrawGoal(Configuration cfg)
    {
        Styling.SectionLabel(Loc.T(L.Setup.RunGoal));
        Styling.VSpace(6f);
        var modes = SeriesGrindModes.All;
        var selected = 0;
        for (var i = 0; i < modes.Count && i < Goals.Length; i++)
        {
            if (cfg.ActiveMode.Id == modes[i].Id) selected = i;
            Goals[i] = modes[i].Id switch
            {
                MatchCountMode.ModeId => new(FontAwesomeIcon.ListOl, Loc.T(L.Plan.TabMatches)),
                SeriesRankMode.ModeId => new(FontAwesomeIcon.Medal, Loc.T(L.Setup.SeriesRank)),
                TimeBoxedMode.ModeId => new(FontAwesomeIcon.Stopwatch, Loc.T(L.Plan.TabTime)),
                _ => new(FontAwesomeIcon.Infinity, Loc.T(L.Plan.TabEndless)),
            };
        }
        if (Segmented.Draw("##session_goal", Goals.AsSpan(0, Math.Min(modes.Count, Goals.Length)), ref selected, height: 36f))
        {
            cfg.ModeId = modes[selected].Id;
            cfg.SaveDebounced();
        }
        Styling.VSpace(6f);
        if (cfg.ActiveMode.Id == EndlessMode.ModeId)
        {
            ChoiceCards.Description(Loc.T(L.Plan.EndlessHelp));
            return;
        }
        var rank = PvpProfileReader.SeriesCurrentRank();
        var minimumRank = SeriesRankMode.LowestOpenTarget(rank);
        var (unit, step, min, max, value, note) = cfg.ActiveMode.Id switch
        {
            MatchCountMode.ModeId => (L.Plan.UnitMatches, 5, 1, 999, cfg.TargetMatchCount, Loc.T(L.Plan.StopAfterMatchesHelp)),
            SeriesRankMode.ModeId => (L.Plan.UnitRank, 1, minimumRank, SeriesRankMode.MaxRank, Math.Max(cfg.TargetSeriesRank, minimumRank),
                Loc.T(L.Plan.ReachRankHelp, rank) + " " + Loc.T(L.Setup.SeriesGoalHint)),
            _ => (L.Plan.UnitMinutes, 5, 1, 1440, cfg.TargetMinutes, Loc.T(L.Plan.StopAfterTimeHelp)),
        };
        if (Stepper.Draw("##session_target", ref value, step, min, max, "%d"))
        {
            switch (cfg.ActiveMode.Id)
            {
                case MatchCountMode.ModeId: cfg.TargetMatchCount = value; break;
                case SeriesRankMode.ModeId: cfg.TargetSeriesRank = value; break;
                default: cfg.TargetMinutes = value; break;
            }
            cfg.SaveDebounced();
        }
        ImGui.SameLine(0f, 10f * ImGuiHelpers.GlobalScale);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(Loc.T(unit));
        Styling.VSpace(4f);
        ChoiceCards.Description(note);
    }

    private static void DrawAfter(Configuration cfg)
    {
        Styling.SectionLabel(Loc.T(L.Plan.AfterGoalTitle));
        Styling.VSpace(6f);
        for (var i = 0; i < After.Length; i++) After[i] = new(AfterChoices[i].Icon, Loc.T(AfterChoices[i].Label));
        var selected = Math.Max(0, Array.IndexOf(Actions, cfg.AfterRun));
        var enabled = cfg.ActiveMode.Id != EndlessMode.ModeId;
        for (var row = 0; row < 2; row++)
        {
            var local = selected / 2 == row ? selected % 2 : -1;
            if (Segmented.Draw($"##after_run_{row}", After.AsSpan(row * 2, 2), ref local, enabled, height: 34f, allowNone: true))
            {
                selected = row * 2 + local;
                cfg.AfterRun = Actions[selected];
                cfg.SaveDebounced();
                ImGui.CloseCurrentPopup();
            }
        }
        Styling.VSpace(4f);
        ChoiceCards.Description(Loc.T(enabled ? AfterChoices[selected].Help : L.Setup.AfterEndless));
    }
}
