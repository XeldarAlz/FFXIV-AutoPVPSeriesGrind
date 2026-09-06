using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Modes;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Shell;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class RunningPanel
{
    private const float MatchClockSeconds = Core.ApsgConstants.CrystallineConflict.MatchLengthSec;
    private const float PadX = 18f;

    private readonly record struct GoalInfo(float? Fraction, string CenterBig, string? CenterSmall, string Remaining, bool Endless);

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var stage = ReadyState.ResolveStage(ctrl);
        var (accent, accentSoft, label) = ReadyState.StagePalette(stage);

        DrawHeaderStrip(accent, accentSoft, stage);
        Styling.VSpace(6f);
        DrawHeroCard(cfg, ctrl, stage, accent, accentSoft, label);
        Styling.VSpace(10f);

        if (BrainPanel.Draw(cfg, ctrl)) Styling.VSpace(10f);
        DrawStatTiles(cfg, ctrl);
    }

    private static void DrawHeaderStrip(Vector4 accent, Vector4 accentSoft, ReadyState.Stage stage)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var lineHeight = ImGui.GetTextLineHeight();
        var midY = origin.Y + lineHeight * 0.5f;

        var radius = 4f * scale;
        Paint.Dot(dl, new Vector2(origin.X + radius + 3f * scale, midY), radius,
            Styling.PulseColor(accent, accentSoft, Styling.PulseMedium));

        var status = Loc.T(L.Run.Running);
        var statusSize = TextDraw.SmallCapsSize(status);
        TextDraw.SmallCaps(status, new Vector2(origin.X + radius * 2f + 12f * scale, midY - statusSize.Y * 0.5f), Styling.TextSecondary);

        var footer = stage is ReadyState.Stage.Preparing or ReadyState.Stage.Queueing
            ? Loc.T(L.Run.ModeCasual)
            : MiniPlayer.CurrentMapName();
        using (Fonts.PushCaption())
        {
            var footerSize = TextDraw.Measure(footer);
            TextDraw.At(footer, new Vector2(origin.X + avail - footerSize.X, midY - footerSize.Y * 0.5f), Styling.TextMuted);
        }

        ImGui.Dummy(new Vector2(avail, lineHeight));
    }

    private static void DrawHeroCard(Configuration cfg, AutoPvpSeriesController ctrl, ReadyState.Stage stage,
        Vector4 accent, Vector4 accentSoft, string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.HeroCardHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var rounding = Styling.PanelRounding * scale;
        var fighting = stage == ReadyState.Stage.Fighting;

        Paint.Glass(dl, origin, end, rounding, accent, 0.10f, 0f, elevated: true);
        var border = Styling.PulseColor(Styling.WithAlpha(accent, 0.5f), accentSoft, fighting ? Styling.PulseFast : Styling.PulseMedium);
        Paint.Stroke(dl, origin, end, border, rounding, 1.6f);

        var goal = ResolveGoal(cfg, ctrl);

        var padX = PadX * scale;
        var ringRadius = size.Y * 0.5f - 18f * scale;
        var ringCenter = new Vector2(origin.X + padX + ringRadius, origin.Y + size.Y * 0.5f);
        DrawGoalRing(ringCenter, ringRadius, accent, accentSoft, stage, goal);

        var columnX = ringCenter.X + ringRadius + 20f * scale;
        var columnRight = end.X - padX;
        var columnWidth = columnRight - columnX;
        var y = origin.Y + 20f * scale;

        var chipHeight = DrawPhaseChip(columnX, y, label, accent, accentSoft);
        y += chipHeight + 10f * scale;

        var inDuty = Svc.Condition[ConditionFlag.BoundByDuty];
        var timeLeft = inDuty ? DutyOps.ContentTimeLeft() : 0;
        var inMatch = stage is ReadyState.Stage.Fighting or ReadyState.Stage.InMatch && timeLeft > 0;

        var line = HeadlineFor(ctrl, stage, inMatch);
        using (Fonts.PushHeadline())
        {
            var text = TextDraw.Truncate(line, columnWidth);
            var textSize = TextDraw.Measure(text);
            TextDraw.At(text, new Vector2(columnX, y), Styling.TextStrong);
            y += textSize.Y + 10f * scale;
        }

        var barHeight = 10f * scale;
        if (inMatch)
        {
            var remaining = Motion.Approach(Motion.Key("##apsg_match_clock"), Math.Clamp(timeLeft / MatchClockSeconds, 0f, 1f), 10f);
            Paint.Bar(dl, new Vector2(columnX, y), columnWidth, barHeight, remaining, accent);
        }
        else
        {
            Paint.IndeterminateBar(dl, new Vector2(columnX, y), columnWidth, barHeight, accent);
        }

        y += barHeight + 8f * scale;

        using (Fonts.PushCaption())
        {
            var left = inMatch
                ? Loc.T(L.Run.TimeLeft, Formatting.Time(timeLeft))
                : TextDraw.Truncate(ctrl.Status, columnWidth * 0.6f);
            TextDraw.At(left, new Vector2(columnX, y), Styling.TextDim);
            TextDraw.Right(goal.Remaining, columnRight, y, Styling.WithAlpha(accentSoft, 0.9f));
        }

        ImGui.Dummy(size);
    }

    private static string HeadlineFor(AutoPvpSeriesController ctrl, ReadyState.Stage stage, bool inMatch)
    {
        if (inMatch)
        {
            var job = ctrl.SessionSnapshot?.JobAbbr;
            var prefix = string.IsNullOrEmpty(job) ? string.Empty : Loc.T(L.Run.JobPrefix, job);
            return prefix + MiniPlayer.CurrentMapName();
        }

        return stage switch
        {
            ReadyState.Stage.Preparing => Loc.T(L.Run.HeadlinePreparing),
            ReadyState.Stage.Queueing  => Loc.T(L.Run.HeadlineInQueue),
            ReadyState.Stage.Portraits => Loc.T(L.Run.HeadlineMatchStarting, MiniPlayer.CurrentMapName()),
            ReadyState.Stage.Finishing => Loc.T(L.Run.HeadlineWrappingUp),
            _                          => string.IsNullOrWhiteSpace(ctrl.Status) ? Loc.T(L.Run.HeadlineWorking) : ctrl.Status,
        };
    }

    private static void DrawGoalRing(Vector2 center, float radius, Vector4 accent, Vector4 accentSoft, ReadyState.Stage stage, GoalInfo goal)
    {
        var thickness = 6f * ImGuiHelpers.GlobalScale;
        ProgressRing.Track(center, radius, thickness, Styling.WithAlpha(Styling.BorderDim, 0.7f));

        if (stage == ReadyState.Stage.Finishing)
        {
            ProgressRing.Fill(center, radius, thickness, 1f, accent);
            ProgressRing.Sweep(center, radius, thickness * 0.55f, accentSoft, Styling.PulseOrbit, 1.0f, 0.40f);
            ProgressRing.CenterIcon(center, FontAwesomeIcon.Check, accentSoft, radius * 0.7f);
            return;
        }

        if (goal.Endless)
        {
            ProgressRing.Sweep(center, radius, thickness, accentSoft, Styling.PulseOrbit, MathF.PI * 0.6f, 1f);
        }
        else
        {
            var fraction = Motion.Approach(Motion.Key("##apsg_goal_ring"), goal.Fraction ?? 0f, 6f);
            ProgressRing.Fill(center, radius, thickness, fraction, accent);
            ProgressRing.Sweep(center, radius, thickness * 0.5f, accentSoft, Styling.PulseOrbit, 0.85f, 0.25f);
        }

        ProgressRing.CenterValue(center, goal.CenterBig, goal.CenterSmall, Styling.TextStrong, goal.Endless ? accentSoft : Styling.TextDim);
    }

    private static float DrawPhaseChip(float x, float y, string text, Vector4 accent, Vector4 accentSoft)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var padX = 9f * scale;
        var padY = 3f * scale;

        using (Fonts.PushCaption())
        {
            var label = TextDraw.Upper(text);
            var textSize = TextDraw.Measure(label);
            var chipMin = new Vector2(x, y);
            var chipMax = chipMin + new Vector2(padX * 2f + textSize.X, textSize.Y + padY * 2f);

            Paint.Pill(dl, chipMin, chipMax, Styling.WithAlpha(accent, 0.28f), Styling.WithAlpha(accent, 0.65f));
            TextDraw.At(label, new Vector2(x + padX, y + padY), accentSoft);
            return chipMax.Y - chipMin.Y;
        }
    }

    private static GoalInfo ResolveGoal(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var session = ctrl.SessionSnapshot;
        var matches = session?.MatchesCompleted ?? 0;

        switch (cfg.ActiveMode.Id)
        {
            case MatchCountMode.ModeId:
            {
                var target = Math.Max(1, cfg.TargetMatchCount);
                var left = Math.Max(0, target - matches);
                return new GoalInfo(matches / (float)target, matches.ToString(), Loc.T(L.Run.GoalOf, target),
                    left == 0 ? Loc.T(L.Run.GoalReached) : Loc.T(L.Run.GoalToGo, left), false);
            }
            case TimeBoxedMode.ModeId:
            {
                var target = Math.Max(1, cfg.TargetMinutes);
                var elapsed = session?.Elapsed.TotalMinutes ?? 0;
                var left = Math.Max(0, target - (int)elapsed);
                return new GoalInfo((float)(elapsed / target), ((int)elapsed).ToString(), Loc.T(L.Run.GoalOfMinutes, target),
                    left == 0 ? Loc.T(L.Run.GoalReached) : Loc.T(L.Run.GoalMinutesToGo, left), false);
            }
            case SeriesRankMode.ModeId:
            {
                var rank = PvpProfileReader.SeriesCurrentRank();
                var left = Math.Max(0, cfg.TargetSeriesRank - rank);
                return new GoalInfo(PvpProfileReader.SeriesRankProgress(), rank.ToString(), Loc.T(L.Run.GoalToRank, cfg.TargetSeriesRank),
                    left == 0 ? Loc.T(L.Run.GoalReached) : Loc.T(L.Run.GoalRanksToGo, left), false);
            }
            default:
                // Endless has no goal fill, so the rotating comet is the hero and the centre reads
                // as "matches so far, forever".
                return new GoalInfo(null, matches.ToString(), "∞", Loc.T(L.Run.GoalEndless), true);
        }
    }

    private static void DrawStatTiles(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var session = ctrl.SessionSnapshot;
        var scale = ImGuiHelpers.GlobalScale;
        var avail = ImGui.GetContentRegionAvail().X;
        var gap = 8f * scale;
        var tileWidth = (avail - gap * 3f) / 4f;

        var matches = session?.MatchesCompleted ?? 0;
        var elapsed = session?.Elapsed ?? TimeSpan.Zero;
        var perHour = elapsed.TotalHours > 0 ? matches / elapsed.TotalHours : 0;
        var seriesExp = session?.SeriesExpGained ?? 0;
        var expPerHour = elapsed.TotalHours > 0 ? seriesExp / elapsed.TotalHours : 0;

        StatTile.Draw(Loc.T(L.Run.TileMatches), matches.ToString(), null, Styling.AccentBlue, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.Run.TileSeriesExp), $"+{Formatting.Exp(seriesExp)}", expPerHour >= 1 ? $"{Formatting.Exp((long)expPerHour)}/h" : null, Styling.AccentAmber, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.Run.TileMatchesPerHour), perHour > 0 ? perHour.ToString("F1") : "–", null, Styling.AccentMint, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.Run.TileElapsed), Formatting.Elapsed(elapsed), ReadyState.StopSummary(cfg), Styling.AccentViolet, tileWidth);
    }
}
