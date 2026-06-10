using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Modes;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class IdlePanel
{
    private const int PayoffMs = 5000;

    private static readonly Dictionary<string, (FontAwesomeIcon Icon, string Label)> modeVisuals = new()
    {
        [MatchCountMode.ModeId] = (FontAwesomeIcon.ListOl, "Matches"),
        [SeriesRankMode.ModeId] = (FontAwesomeIcon.Medal, "Rank"),
        [TimeBoxedMode.ModeId]  = (FontAwesomeIcon.Stopwatch, "Time"),
        [EndlessMode.ModeId]    = (FontAwesomeIcon.Infinity, "Endless"),
    };

    private static readonly AfterRunAction[] afterRunOrder =
        [AfterRunAction.StayLoggedIn, AfterRunAction.ReturnToInn, AfterRunAction.Logout, AfterRunAction.CloseGame];

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        if (DrawPayoff(ctrl)) return;

        var s = ImGuiHelpers.GlobalScale;
        var avail = ImGui.GetContentRegionAvail().X;
        var colW = MathF.Min(avail, 420f * s);
        var x0 = ImGui.GetCursorPosX() + (avail - colW) * 0.5f;

        Styling.VSpace(6);
        DrawActivity(x0, colW, s);
        DrawJobHint(s);
        Styling.VSpace(16);
        DrawModeRow(cfg, x0, colW, s);
        Styling.VSpace(12);
        DrawTarget(cfg, s);
        Styling.VSpace(22);
        DrawPlayHero(ctrl, s);
        Styling.VSpace(8);
        DrawThen(cfg, s);
    }

    private static void DrawActivity(float x0, float colW, float s)
    {
        var gap = 8f * s;
        var size = new Vector2((colW - gap) / 2f, ImGui.GetFrameHeight() * 1.2f);

        ImGui.SetCursorPosX(x0);
        Segment("##act_cc", FontAwesomeIcon.Trophy, "Crystalline Conflict", Styling.AccentViolet,
            selected: true, disabled: false, size, "Auto-queue Crystalline Conflict casual matches to grind your PvP Series.");
        ImGui.SameLine(0, gap);
        Segment("##act_fl", FontAwesomeIcon.Flag, "Frontline", Styling.AccentVioletSoft,
            selected: false, disabled: true, size, "Frontline queueing is on the way.\nComing soon, stay tuned!");
    }

    private static void DrawJobHint(float s)
    {
        if (!MatchState.LocalIsMelee()) return;

        Styling.VSpace(8);

        const string text = "Ranged jobs grind more efficiently than melee";
        var icon = FontAwesomeIcon.Lightbulb.ToIconString();
        var col = Styling.AccentAmberSoft;
        var gap = 6f * s;

        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(icon);
        var totalW = iconSize.X + gap + ImGui.CalcTextSize(text).X;
        Styling.CenterNextItem(totalW);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, col))
            ImGui.TextUnformatted(icon);
        ImGui.SameLine(0, gap);
        using (ImRaii.PushColor(ImGuiCol.Text, col))
            ImGui.TextUnformatted(text);
    }

    private static void DrawModeRow(Configuration cfg, float x0, float colW, float s)
    {
        var modes = SeriesGrindModes.All;
        var gap = 6f * s;
        var segW = (colW - gap * (modes.Count - 1)) / modes.Count;
        var size = new Vector2(segW, ImGui.GetFrameHeight() * 1.55f);
        var activeId = cfg.ActiveMode.Id;

        ImGui.SetCursorPosX(x0);
        for (var i = 0; i < modes.Count; i++)
        {
            if (i > 0) ImGui.SameLine(0, gap);
            var mode = modes[i];
            var (icon, lbl) = modeVisuals.TryGetValue(mode.Id, out var v) ? v : (FontAwesomeIcon.Flag, mode.DisplayName);
            if (Segment($"##mode_{mode.Id}", icon, lbl, Styling.AccentViolet, mode.Id == activeId, false, size, mode.Description))
            {
                cfg.ModeId = mode.Id;
                cfg.SaveDebounced();
            }
        }
    }

    private static void DrawTarget(Configuration cfg, float s)
    {
        switch (cfg.ActiveMode.Id)
        {
            case MatchCountMode.ModeId:
            {
                Styling.TextCentered("STOP AFTER", Styling.TextSecondary, 1.1f);
                Styling.VSpace(5);
                var v = cfg.TargetMatchCount;
                if (CenteredStepper("matches", ref v, 1, 999, 1, s)) { cfg.TargetMatchCount = v; cfg.SaveDebounced(); }
                Styling.VSpace(3);
                Styling.TextCentered("matches", Styling.TextSecondary, 1.1f);
                break;
            }
            case SeriesRankMode.ModeId:
            {
                var cur = PvpProfileReader.SeriesCurrentRank();
                var min = Math.Clamp(cur + 1, 1, 30);
                Styling.TextCentered("REACH SERIES RANK", Styling.TextSecondary, 1.1f);
                Styling.VSpace(5);
                var v = Math.Max(cfg.TargetSeriesRank, min);
                var changed = CenteredStepper("rank", ref v, min, 30, 1, s);
                if (changed || v != cfg.TargetSeriesRank) { cfg.TargetSeriesRank = v; cfg.SaveDebounced(); }
                Styling.VSpace(3);
                Styling.TextCentered($"you're rank {cur} now", Styling.TextSecondary, 1.1f);
                break;
            }
            case TimeBoxedMode.ModeId:
            {
                Styling.TextCentered("STOP AFTER", Styling.TextSecondary, 1.1f);
                Styling.VSpace(5);
                var v = cfg.TargetMinutes;
                if (CenteredStepper("minutes", ref v, 1, 1440, 5, s)) { cfg.TargetMinutes = v; cfg.SaveDebounced(); }
                Styling.VSpace(3);
                Styling.TextCentered("minutes", Styling.TextSecondary, 1.1f);
                break;
            }
            default:
                Styling.TextCentered("Queues match after match until you press Stop.", Styling.TextSecondary, 1.1f);
                break;
        }
    }

    private static void DrawPlayHero(AutoPvpSeriesController ctrl, float s)
    {
        var radius = 52f * s;
        var leftX = ImGui.GetCursorPosX();
        var startScreen = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availX * 0.5f, startScreen.Y + radius + 2f * s);

        var ready = ExternalPlugins.AllRequiredInstalled();
        if (ProgressRing.PlayButton(center, radius, ready))
            ctrl.Start();

        ImGui.SetCursorPosX(leftX);
        Styling.VSpace(5);
        if (ready)
            Styling.TextCentered("START", Styling.AccentVioletSoft, 0.95f);
        else
            Styling.TextCentered("Install the required plugins to start", Styling.TextMuted, 0.9f);
    }

    private static void DrawThen(Configuration cfg, float s)
    {
        if (cfg.ActiveMode.Id == EndlessMode.ModeId) return;

        Styling.VSpace(4);
        var preview = $"When done  ·  {AfterRunLabel(cfg.AfterRun)}";
        var w = ImGui.CalcTextSize(preview).X + ImGui.GetFrameHeight() + 18f * s;
        Styling.CenterNextItem(w);
        ImGui.SetNextItemWidth(w);

        using (ImRaii.PushColor(ImGuiCol.FrameBg, Styling.CardBgSoft)
            .Push(ImGuiCol.FrameBgHovered, Styling.CardBgHover)
            .Push(ImGuiCol.Text, Styling.TextDim)
            .Push(ImGuiCol.Border, Styling.BorderDim))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        using (var combo = ImRaii.Combo("##afterrun", preview))
        {
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
        }
    }

    private static bool DrawPayoff(AutoPvpSeriesController ctrl)
    {
        if (!ctrl.LastByGoal || !ctrl.HasRecentResult(PayoffMs)) return false;

        var s = ImGuiHelpers.GlobalScale;
        var radius = 58f * s;
        var thickness = 7f * s;

        Styling.VSpace(26);
        Styling.TextCentered("·   DONE   ·", Styling.AccentAmberSoft, 0.95f);
        Styling.VSpace(12);

        var startScreen = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availX * 0.5f, startScreen.Y + radius + 4f * s);
        ProgressRing.Glow(center, radius, Styling.AccentAmber, 0.6f + 0.4f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.85f));
        ProgressRing.Fill(center, radius, thickness, 1f, Styling.AccentAmber);
        ProgressRing.CenterIcon(center, FontAwesomeIcon.Check, Styling.AccentAmberSoft, radius * 0.62f);
        ImGui.SetCursorScreenPos(startScreen);
        ImGui.Dummy(new Vector2(availX, radius * 2f + 8f * s));

        Styling.VSpace(12);
        var line = $"{ctrl.LastMatches} matches";
        if (ctrl.LastSeriesExp > 0) line += $"     ·     +{Formatting.Exp(ctrl.LastSeriesExp)} Series EXP";
        Styling.TextCentered(line, Styling.TextSecondary);
        Styling.VSpace(8);
        Styling.TextCentered("click anywhere to continue", Styling.TextMuted, 0.85f);

        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            ctrl.ClearLastResult();
        return true;
    }

    private static bool CenteredStepper(string id, ref int value, int min, int max, int step, float s)
    {
        var h = ImGui.GetFrameHeight();
        var inputW = 58f * s;
        var inner = 4f * s;
        var width = h + inner + inputW + inner + h;
        Styling.CenterNextItem(width);

        var changed = false;
        using (ImRaii.PushColor(ImGuiCol.Button, Styling.CardBgSoft)
            .Push(ImGuiCol.ButtonHovered, Vector4.Lerp(Styling.CardBg, Styling.AccentViolet, 0.30f))
            .Push(ImGuiCol.ButtonActive, Styling.AccentViolet * 0.7f)
            .Push(ImGuiCol.Border, Styling.BorderDim))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(inner, ImGui.GetStyle().ItemSpacing.Y)))
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
            ImGui.SetNextItemWidth(inputW);
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

    private static bool Segment(string id, FontAwesomeIcon icon, string label, Vector4 accent,
        bool selected, bool disabled, Vector2 size, string? tooltip)
    {
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var rawHover = ImGui.IsMouseHoveringRect(origin, end);
        var hover = !disabled && rawHover;
        var s = ImGuiHelpers.GlobalScale;

        var bg = disabled ? Styling.CardBgSoft
            : selected ? Vector4.Lerp(Styling.CardBg, accent, 0.22f)
            : hover ? Styling.CardBgHover : Styling.CardBgSoft;
        var border = disabled ? Styling.BorderDim
            : selected ? accent : hover ? accent * 0.5f : Styling.BorderDim;
        var textCol = disabled ? Styling.TextMuted : selected ? Styling.TextStrong : Styling.TextSecondary;
        var iconCol = disabled ? Styling.TextMuted : selected ? accent : Styling.TextSecondary;

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(bg), 6f);
        dl.AddRect(origin, end, ImGui.GetColorU32(border), 6f, ImDrawFlags.None, selected ? 2f : 1f);

        var iconStr = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);
        var labelSize = ImGui.CalcTextSize(label);
        var innerGap = 6f * s;
        var contentW = iconSize.X + innerGap + labelSize.X;
        var startX = origin.X + MathF.Max(4f * s, (size.X - contentW) * 0.5f);
        var midY = origin.Y + size.Y * 0.5f;

        ImGui.SetCursorScreenPos(new Vector2(startX, midY - iconSize.Y * 0.5f));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, iconCol))
            ImGui.TextUnformatted(iconStr);

        ImGui.SetCursorScreenPos(new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, textCol))
            ImGui.TextUnformatted(label);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);

        if (rawHover && !string.IsNullOrEmpty(tooltip))
            ImGui.SetTooltip(tooltip);
        if (hover)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) return true;
        }
        return false;
    }

    private static string AfterRunLabel(AfterRunAction a) => a switch
    {
        AfterRunAction.StayLoggedIn => "stay where you are",
        AfterRunAction.ReturnToInn  => "return to the inn",
        AfterRunAction.Logout       => "log out to title",
        AfterRunAction.CloseGame    => "close the game",
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
}
