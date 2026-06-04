using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Modes;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

// "Run until" mode picker: a segmented control over the stop modes, the matching target input, and a
// one-line plain-language plan. Mirrors the FATE plugin's goal summary.
internal static class GoalSelector
{
    private static readonly Dictionary<string, (FontAwesomeIcon Icon, string Label)> visuals = new()
    {
        [MatchCountMode.ModeId] = (FontAwesomeIcon.ListOl, "Matches"),
        [SeriesRankMode.ModeId] = (FontAwesomeIcon.Medal, "Series rank"),
        [TimeBoxedMode.ModeId]  = (FontAwesomeIcon.Stopwatch, "Time"),
        [EndlessMode.ModeId]    = (FontAwesomeIcon.Infinity, "Endless"),
    };

    public static void Draw(Configuration cfg)
    {
        ImGui.Spacing();
        Styling.SectionLabel("Run until");
        ImGui.Spacing();

        DrawSelector(cfg);
        ImGui.Spacing();
        DrawTargetRow(cfg);
        ImGui.Spacing();
        DrawThen(cfg);
        DrawPlan(cfg);
    }

    private static readonly AfterRunAction[] afterRunOrder =
        [AfterRunAction.StayLoggedIn, AfterRunAction.ReturnToInn, AfterRunAction.Logout, AfterRunAction.CloseGame];

    private static void DrawThen(Configuration cfg)
    {
        // Endless never completes by goal, so there's no "after" — hide the section rather than imply one.
        if (cfg.ActiveMode.Id == EndlessMode.ModeId) return;

        Styling.SectionLabel("Then");
        ImGui.Spacing();

        ImGui.SetNextItemWidth(220f * ImGuiHelpers.GlobalScale);
        using (var combo = ImRaii.Combo("##afterrun", AfterRunLabel(cfg.AfterRun)))
            if (combo)
                foreach (var a in afterRunOrder)
                {
                    if (ImGui.Selectable(AfterRunLabel(a), a == cfg.AfterRun))
                    {
                        cfg.AfterRun = a;
                        cfg.SaveDebounced();
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip(AfterRunTooltip(a));
                }

        ImGui.Spacing();
    }

    private static string AfterRunLabel(AfterRunAction a) => a switch
    {
        AfterRunAction.StayLoggedIn => "Stay where you are",
        AfterRunAction.ReturnToInn  => "Return to the inn",
        AfterRunAction.Logout       => "Log out to title",
        AfterRunAction.CloseGame    => "Close the game",
        _                           => a.ToString(),
    };

    private static string AfterRunTooltip(AfterRunAction a) => a switch
    {
        AfterRunAction.StayLoggedIn => "Just stop. You're left standing wherever the last match dropped you.",
        AfterRunAction.ReturnToInn  => "Travel to the inn and enter your room (via Lifestream).",
        AfterRunAction.Logout       => "Log out to the title screen.",
        AfterRunAction.CloseGame    => "Close FFXIV entirely (via XIVLauncher's /xlkill).",
        _                           => "",
    };

    private static void DrawSelector(Configuration cfg)
    {
        var modes = SeriesGrindModes.All;
        var activeId = cfg.ActiveMode.Id;
        var avail = ImGui.GetContentRegionAvail().X;
        var gap = 6f * ImGuiHelpers.GlobalScale;
        var segW = (avail - gap * (modes.Count - 1)) / modes.Count;
        var segH = ImGui.GetFrameHeight() * 1.3f;

        for (var i = 0; i < modes.Count; i++)
        {
            if (i > 0) ImGui.SameLine(0, gap);
            var mode = modes[i];
            var (icon, label) = visuals.TryGetValue(mode.Id, out var v) ? v : (FontAwesomeIcon.Flag, mode.DisplayName);
            if (DrawSegment(icon, label, mode.Id == activeId, new Vector2(segW, segH)))
            {
                cfg.ModeId = mode.Id;
                cfg.SaveDebounced();
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(mode.Description);
        }
    }

    private static bool DrawSegment(FontAwesomeIcon icon, string label, bool selected, Vector2 size)
    {
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var hovered = ImGui.IsMouseHoveringRect(origin, end);

        var bg = selected ? Vector4.Lerp(Styling.CardBg, Styling.AccentViolet, 0.22f)
            : hovered ? Styling.CardBgHover : Styling.CardBgSoft;
        var border = selected ? Styling.AccentViolet : hovered ? Styling.AccentViolet * 0.5f : Styling.BorderDim;
        var textColor = selected ? Styling.TextStrong : Styling.TextSecondary;

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(bg), 6f);
        dl.AddRect(origin, end, ImGui.GetColorU32(border), 6f, ImDrawFlags.None, selected ? 2f : 1f);

        var iconStr = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);
        var labelSize = ImGui.CalcTextSize(label);
        var innerGap = 6f * ImGuiHelpers.GlobalScale;
        var startX = origin.X + (size.X - (iconSize.X + innerGap + labelSize.X)) * 0.5f;
        var midY = origin.Y + size.Y * 0.5f;

        ImGui.SetCursorScreenPos(new Vector2(startX, midY - iconSize.Y * 0.5f));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, selected ? Styling.AccentViolet : textColor))
            ImGui.TextUnformatted(iconStr);

        ImGui.SetCursorScreenPos(new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, textColor))
            ImGui.TextUnformatted(label);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);

        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) return true;
        }
        return false;
    }

    private static void DrawTargetRow(Configuration cfg)
    {
        ImGui.AlignTextToFramePadding();
        switch (cfg.ActiveMode.Id)
        {
            case MatchCountMode.ModeId:
            {
                Caption("Stop after");
                ImGui.SameLine();
                var v = cfg.TargetMatchCount;
                if (Stepper("matches", ref v, 1, 999, 1))
                {
                    cfg.TargetMatchCount = v;
                    cfg.SaveDebounced();
                }
                ImGui.SameLine();
                Dim("matches");
                break;
            }
            case SeriesRankMode.ModeId:
            {
                var current = PvpProfileReader.SeriesCurrentRank();
                var min = Math.Clamp(current + 1, 1, 30);
                Caption("Reach rank");
                ImGui.SameLine();
                // You can only target a rank above your current one — anything at or below is already done.
                var v = Math.Max(cfg.TargetSeriesRank, min);
                var changed = Stepper("rank", ref v, min, 30, 1);
                if (changed || v != cfg.TargetSeriesRank)
                {
                    cfg.TargetSeriesRank = v;
                    cfg.SaveDebounced();
                }
                ImGui.SameLine();
                Dim($"·  currently rank {current}");
                break;
            }
            case TimeBoxedMode.ModeId:
            {
                Caption("Stop after");
                ImGui.SameLine();
                var v = cfg.TargetMinutes;
                if (Stepper("minutes", ref v, 1, 1440, 5))
                {
                    cfg.TargetMinutes = v;
                    cfg.SaveDebounced();
                }
                ImGui.SameLine();
                Dim("minutes");
                break;
            }
            default:
                Dim("Queues match after match until you press Stop.");
                break;
        }
    }

    // − [ value ] + : a typeable number flanked by accent-tinted square step buttons. Returns true on change;
    // value is clamped to [min, max].
    private static bool Stepper(string id, ref int value, int min, int max, int step)
    {
        var s = ImGuiHelpers.GlobalScale;
        var h = ImGui.GetFrameHeight();
        var changed = false;

        using (ImRaii.PushColor(ImGuiCol.Button, Styling.CardBgSoft))
        using (ImRaii.PushColor(ImGuiCol.ButtonHovered, Vector4.Lerp(Styling.CardBg, Styling.AccentViolet, 0.30f)))
        using (ImRaii.PushColor(ImGuiCol.ButtonActive, Styling.AccentViolet * 0.7f))
        using (ImRaii.PushColor(ImGuiCol.Border, Styling.BorderDim))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(4f * s, ImGui.GetStyle().ItemSpacing.Y)))
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.Disabled(value <= min))
                if (ImGui.Button(FontAwesomeIcon.Minus.ToIconString() + $"##{id}_minus", new Vector2(h, h)))
                {
                    value = Math.Max(min, value - step);
                    changed = true;
                }

            ImGui.SameLine();
            var v = value;
            ImGui.SetNextItemWidth(58f * s);
            if (ImGui.InputInt($"##{id}", ref v, 0, 0))
            {
                value = Math.Clamp(v, min, max);
                changed = true;
            }

            ImGui.SameLine();
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.Disabled(value >= max))
                if (ImGui.Button(FontAwesomeIcon.Plus.ToIconString() + $"##{id}_plus", new Vector2(h, h)))
                {
                    value = Math.Min(max, value + step);
                    changed = true;
                }
        }

        return changed;
    }

    private static void DrawPlan(Configuration cfg)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(FontAwesomeIcon.InfoCircle.ToIconString());
        ImGui.SameLine();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextWrapped(PlanSentence(cfg));
    }

    private static string PlanSentence(Configuration cfg)
    {
        if (cfg.ActiveMode.Id == EndlessMode.ModeId)
            return "Queue Casual Match after match until you press Stop.";

        var until = cfg.ActiveMode.Id switch
        {
            MatchCountMode.ModeId => $"for {cfg.TargetMatchCount} matches",
            SeriesRankMode.ModeId => $"until you hit Series rank {cfg.TargetSeriesRank}",
            TimeBoxedMode.ModeId  => $"for {cfg.TargetMinutes} minutes",
            _                     => "until done",
        };
        var then = cfg.AfterRun switch
        {
            AfterRunAction.ReturnToInn => "head to the inn",
            AfterRunAction.Logout      => "log out to the title screen",
            AfterRunAction.CloseGame   => "close the game",
            _                          => "stop and stay where you are",
        };
        return $"Queue Casual Match {until}, then {then}.";
    }

    private static void Caption(string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(text);
    }

    private static void Dim(string text)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(text);
    }
}
