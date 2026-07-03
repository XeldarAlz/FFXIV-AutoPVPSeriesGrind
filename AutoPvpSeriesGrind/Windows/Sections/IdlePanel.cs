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
    private const float PayoffRingThickness = 7f;

    private static readonly Dictionary<string, (FontAwesomeIcon Icon, string Label)> modeVisuals = new()
    {
        [MatchCountMode.ModeId] = (FontAwesomeIcon.ListOl, "Matches"),
        [SeriesRankMode.ModeId] = (FontAwesomeIcon.Medal, "Rank"),
        [TimeBoxedMode.ModeId]  = (FontAwesomeIcon.Stopwatch, "Time"),
        [EndlessMode.ModeId]    = (FontAwesomeIcon.Infinity, "Endless"),
    };

    private static readonly Dictionary<string, string> modeSegmentIds = BuildModeSegmentIds();

    private static readonly AfterRunAction[] afterRunOrder =
        [AfterRunAction.StayLoggedIn, AfterRunAction.ReturnToInn, AfterRunAction.Logout, AfterRunAction.CloseGame];

    private static readonly Action<Configuration, int> applyTargetMatchCount =
        static (configuration, value) => configuration.TargetMatchCount = value;

    private static readonly Action<Configuration, int> applyTargetSeriesRank =
        static (configuration, value) => configuration.TargetSeriesRank = value;

    private static readonly Action<Configuration, int> applyTargetMinutes =
        static (configuration, value) => configuration.TargetMinutes = value;

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        if (DrawPayoff(ctrl))
        {
            return;
        }

        var scale = ImGuiHelpers.GlobalScale;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var columnWidth = MathF.Min(availableWidth, 420f * scale);
        var columnStartX = ImGui.GetCursorPosX() + (availableWidth - columnWidth) * 0.5f;

        Styling.VSpace(6);
        DrawActivity(columnStartX, columnWidth, scale);
        DrawJobHint(scale);
        Styling.VSpace(16);
        DrawModeRow(cfg, columnStartX, columnWidth, scale);
        Styling.VSpace(12);
        DrawTarget(cfg, scale);
        Styling.VSpace(22);
        DrawPlayHero(ctrl, scale);
        Styling.VSpace(8);
        DrawAfterRunSelector(cfg, scale);
    }

    private static void DrawActivity(float columnStartX, float columnWidth, float scale)
    {
        var gap = 8f * scale;
        var size = new Vector2((columnWidth - gap) / 2f, ImGui.GetFrameHeight() * 1.2f);

        ImGui.SetCursorPosX(columnStartX);
        SegmentButton.Draw("##act_cc", FontAwesomeIcon.Trophy, "Crystalline Conflict", Styling.AccentViolet,
            selected: true, disabled: false, size, "Auto-queue Crystalline Conflict casual matches to grind your PvP Series.");
        ImGui.SameLine(0, gap);
        SegmentButton.Draw("##act_fl", FontAwesomeIcon.Flag, "Frontline", Styling.AccentVioletSoft,
            selected: false, disabled: true, size, "Frontline queueing is on the way.\nComing soon, stay tuned!");
    }

    private static void DrawJobHint(float scale)
    {
        if (!MatchState.LocalIsMelee())
        {
            return;
        }

        Styling.VSpace(8);

        const string text = "Ranged jobs grind more efficiently than melee";
        var iconString = Glyph.Of(FontAwesomeIcon.Lightbulb);
        var color = Styling.AccentAmberSoft;
        var gap = 6f * scale;

        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            iconSize = ImGui.CalcTextSize(iconString);
        }

        var totalWidth = iconSize.X + gap + ImGui.CalcTextSize(text).X;
        Styling.CenterNextItem(totalWidth);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(iconString);
        }

        ImGui.SameLine(0, gap);
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(text);
        }
    }

    private static void DrawModeRow(Configuration cfg, float columnStartX, float columnWidth, float scale)
    {
        var modes = SeriesGrindModes.All;
        var gap = 6f * scale;
        var segmentWidth = (columnWidth - gap * (modes.Count - 1)) / modes.Count;
        var size = new Vector2(segmentWidth, ImGui.GetFrameHeight() * 1.55f);
        var activeId = cfg.ActiveMode.Id;

        ImGui.SetCursorPosX(columnStartX);
        for (var modeIndex = 0; modeIndex < modes.Count; modeIndex++)
        {
            if (modeIndex > 0)
            {
                ImGui.SameLine(0, gap);
            }

            var mode = modes[modeIndex];
            var (icon, label) = modeVisuals.TryGetValue(mode.Id, out var visuals) ? visuals : (FontAwesomeIcon.Flag, mode.DisplayName);
            if (SegmentButton.Draw(modeSegmentIds[mode.Id], icon, label, Styling.AccentViolet, mode.Id == activeId, false, size, mode.Description))
            {
                cfg.ModeId = mode.Id;
                cfg.SaveDebounced();
            }
        }
    }

    private static void DrawTarget(Configuration cfg, float scale)
    {
        switch (cfg.ActiveMode.Id)
        {
            case MatchCountMode.ModeId:
                DrawStepperTarget(cfg, scale, "STOP AFTER", "matches", "matches",
                    cfg.TargetMatchCount, cfg.TargetMatchCount, 1, 999, 1, applyTargetMatchCount);
                break;
            case SeriesRankMode.ModeId:
            {
                var currentRank = PvpProfileReader.SeriesCurrentRank();
                var minimumRank = Math.Clamp(currentRank + 1, 1, 30);
                DrawStepperTarget(cfg, scale, "REACH SERIES RANK", $"you're rank {currentRank} now", "rank",
                    Math.Max(cfg.TargetSeriesRank, minimumRank), cfg.TargetSeriesRank, minimumRank, 30, 1, applyTargetSeriesRank);
                break;
            }
            case TimeBoxedMode.ModeId:
                DrawStepperTarget(cfg, scale, "STOP AFTER", "minutes", "minutes",
                    cfg.TargetMinutes, cfg.TargetMinutes, 1, 1440, 5, applyTargetMinutes);
                break;
            default:
                Styling.TextCentered("Queues match after match until you press Stop.", Styling.TextSecondary, 1.1f);
                break;
        }
    }

    private static void DrawStepperTarget(Configuration cfg, float scale, string heading, string caption,
        string stepperId, int initialValue, int storedValue, int minimum, int maximum, int step,
        Action<Configuration, int> applyTarget)
    {
        Styling.TextCentered(heading, Styling.TextSecondary, 1.1f);
        Styling.VSpace(5);

        var value = initialValue;
        var changed = Stepper.DrawCentered(stepperId, ref value, minimum, maximum, step, scale);
        if (changed || value != storedValue)
        {
            applyTarget(cfg, value);
            cfg.SaveDebounced();
        }

        Styling.VSpace(3);
        Styling.TextCentered(caption, Styling.TextSecondary, 1.1f);
    }

    private static void DrawPlayHero(AutoPvpSeriesController ctrl, float scale)
    {
        var radius = 52f * scale;
        var leftX = ImGui.GetCursorPosX();
        var startScreen = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availableWidth * 0.5f, startScreen.Y + radius + 2f * scale);

        var ready = ExternalPlugins.AllRequiredInstalled();
        if (ProgressRing.PlayButton(center, radius, ready))
        {
            ctrl.Start();
        }

        ImGui.SetCursorPosX(leftX);
        Styling.VSpace(5);
        if (ready)
        {
            Styling.TextCentered("START", Styling.AccentVioletSoft, 0.95f);
        }
        else
        {
            Styling.TextCentered("Install the required plugins to start", Styling.TextMuted, 0.9f);
        }
    }

    private static void DrawAfterRunSelector(Configuration cfg, float scale)
    {
        if (cfg.ActiveMode.Id == EndlessMode.ModeId)
        {
            return;
        }

        Styling.VSpace(4);
        var preview = $"When done  ·  {AfterRunLabel(cfg.AfterRun)}";
        var width = ImGui.CalcTextSize(preview).X + ImGui.GetFrameHeight() + 18f * scale;
        Styling.CenterNextItem(width);
        ImGui.SetNextItemWidth(width);

        using (ImRaii.PushColor(ImGuiCol.FrameBg, Styling.CardBgSoft)
            .Push(ImGuiCol.FrameBgHovered, Styling.CardBgHover)
            .Push(ImGuiCol.Text, Styling.TextDim)
            .Push(ImGuiCol.Border, Styling.BorderDim))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f))
        using (var combo = ImRaii.Combo("##afterrun", preview))
        {
            if (combo)
            {
                for (var actionIndex = 0; actionIndex < afterRunOrder.Length; actionIndex++)
                {
                    var action = afterRunOrder[actionIndex];
                    if (ImGui.Selectable(AfterRunLabel(action), action == cfg.AfterRun))
                    {
                        cfg.AfterRun = action;
                        cfg.SaveDebounced();
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(AfterRunTooltip(action));
                    }
                }
            }
        }
    }

    private static bool DrawPayoff(AutoPvpSeriesController ctrl)
    {
        if (!ctrl.LastByGoal || !ctrl.HasRecentResult(PayoffMs))
        {
            return false;
        }

        var scale = ImGuiHelpers.GlobalScale;

        Styling.VSpace(26);
        Styling.TextCentered("·   DONE   ·", Styling.AccentAmberSoft, 0.95f);
        Styling.VSpace(12);

        DrawPayoffRing(scale);

        Styling.VSpace(12);
        DrawPayoffSummary(ctrl);

        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            ctrl.ClearLastResult();
        }

        return true;
    }

    private static void DrawPayoffRing(float scale)
    {
        var radius = Layout.HeroRingRadius * scale;
        var thickness = PayoffRingThickness * scale;

        var startScreen = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availableWidth * 0.5f, startScreen.Y + radius + 4f * scale);
        ProgressRing.Glow(center, radius, Styling.AccentAmber, 0.6f + 0.4f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, radius, thickness, Layout.RingTrackColor);
        ProgressRing.Fill(center, radius, thickness, 1f, Styling.AccentAmber);
        ProgressRing.CenterIcon(center, FontAwesomeIcon.Check, Styling.AccentAmberSoft, radius * Layout.HeroRingIconRatio);
        ImGui.SetCursorScreenPos(startScreen);
        ImGui.Dummy(new Vector2(availableWidth, radius * 2f + 8f * scale));
    }

    private static void DrawPayoffSummary(AutoPvpSeriesController ctrl)
    {
        var line = $"{ctrl.LastMatches} matches";
        if (ctrl.LastSeriesExp > 0)
        {
            line += $"{Layout.WideDotSeparator}+{Formatting.Exp(ctrl.LastSeriesExp)} Series EXP";
        }

        Styling.TextCentered(line, Styling.TextSecondary);
        Styling.VSpace(8);
        Styling.TextCentered("click anywhere to continue", Styling.TextMuted, 0.85f);
    }

    private static Dictionary<string, string> BuildModeSegmentIds()
    {
        var modes = SeriesGrindModes.All;
        var segmentIds = new Dictionary<string, string>(modes.Count);
        for (var modeIndex = 0; modeIndex < modes.Count; modeIndex++)
        {
            var modeId = modes[modeIndex].Id;
            segmentIds[modeId] = $"##mode_{modeId}";
        }

        return segmentIds;
    }

    private static string AfterRunLabel(AfterRunAction action) => action switch
    {
        AfterRunAction.StayLoggedIn => "stay where you are",
        AfterRunAction.ReturnToInn  => "return to the inn",
        AfterRunAction.Logout       => "log out to title",
        AfterRunAction.CloseGame    => "close the game",
        _                           => action.ToString(),
    };

    private static string AfterRunTooltip(AfterRunAction action) => action switch
    {
        AfterRunAction.StayLoggedIn => "Just stop. You're left standing wherever the last match dropped you.",
        AfterRunAction.ReturnToInn  => "Travel to the inn and enter your room (via Lifestream).",
        AfterRunAction.Logout       => "Log out to the title screen.",
        AfterRunAction.CloseGame    => "Close FFXIV entirely (via XIVLauncher's /xlkill).",
        _                           => "",
    };
}
