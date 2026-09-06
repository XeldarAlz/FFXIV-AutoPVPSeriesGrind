using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Stats;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed class HistoryPage
{
    private const int ChartRuns = 24;
    private const float PadX = 14f;
    private const float MetricWidth = 76f;
    private const float ConfirmSlide = 12f;

    private bool confirmClear;

    public void Draw(Plugin plugin)
    {
        var history = plugin.History;
        var totals = history.Lifetime;
        var subtitle = history.Records.Count == 0
            ? Loc.T(L.History.Empty)
            : Loc.T(L.History.Summary, history.Records.Count, Formatting.Elapsed(totals.Duration), totals.MatchesPerHour.ToString("F1", Loc.Culture));
        PageHeader.Draw(Loc.T(L.History.Title), subtitle);

        DrawLifetime(totals);
        Styling.VSpace(14f);

        if (history.Records.Count == 0)
        {
            DrawEmptyState();
            return;
        }

        Label(Loc.T(L.History.ChartTitle));
        DrawChart(history);
        Styling.VSpace(12f);

        Label(Loc.T(L.History.RecentRuns));
        var records = history.Records;
        for (var index = 0; index < records.Count; index++)
        {
            DrawRow(records[index], index);
            Styling.VSpace(2f);
        }

        Styling.VSpace(8f);
        DrawClearControl(history);
    }

    private static void Label(string text)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var size = TextDraw.SectionTitleSize(text);
        TextDraw.SectionTitle(text, origin, Styling.TextStrong);
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, size.Y + 8f * scale));
    }

    private static void DrawLifetime(RunHistory.LifetimeTotals totals)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = 8f * scale;
        var avail = ImGui.GetContentRegionAvail().X;
        var tileWidth = (avail - gap * 3f) / 4f;

        StatTile.Draw(Loc.T(L.History.TileRuns), totals.Runs.ToString("N0", Loc.Culture), null, Styling.AccentViolet, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.History.TileMatches), totals.Matches.ToString("N0", Loc.Culture),
            totals.MatchesPerHour > 0 ? $"{totals.MatchesPerHour:F1}/h" : null, Styling.AccentBlue, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.History.TileSeriesExp), Formatting.Exp(totals.SeriesExp), null, Styling.AccentAmber, tileWidth);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.History.TileTimeGrinding), Formatting.Elapsed(totals.Duration), null, Styling.AccentMint, tileWidth);
    }

    private static void DrawEmptyState()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, 110f * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        Paint.Surface(dl, origin, end, Styling.CardRounding * scale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);

        var center = new Vector2((origin.X + end.X) * 0.5f, origin.Y + size.Y * 0.42f);
        ProgressRing.CenterIcon(center, FontAwesomeIcon.History, Styling.TextMuted, 26f * scale);
        TextDraw.Center(Loc.T(L.History.EmptyDetail), center.X, center.Y + 22f * scale, Styling.TextMuted);
        ImGui.Dummy(size);
    }

    private static void DrawChart(RunHistory history)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var records = history.Records;
        var count = Math.Min(ChartRuns, records.Count);
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.ChartHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();

        Paint.Surface(dl, origin, end, Styling.CardRounding * scale, Styling.WithAlpha(Styling.Surface0, 0.6f), Styling.WithAlpha(Styling.BorderDim, 0.5f), topLight: false);

        var padX = PadX * scale;
        var plotMin = new Vector2(origin.X + padX, origin.Y + 28f * scale);
        var plotMax = new Vector2(end.X - padX, end.Y - 12f * scale);
        var plotWidth = plotMax.X - plotMin.X;
        var plotHeight = plotMax.Y - plotMin.Y;

        var peak = 1;
        for (var index = 0; index < count; index++) peak = Math.Max(peak, records[index].MatchesCompleted);

        using (Fonts.PushCaption())
        {
            TextDraw.At(Loc.T(L.History.LastRuns, count), new Vector2(plotMin.X, origin.Y + 9f * scale), Styling.TextMuted);
            TextDraw.Right(Loc.T(L.History.Best, peak), plotMax.X, origin.Y + 9f * scale, Styling.TextMuted);
        }

        dl.AddLine(new Vector2(plotMin.X, plotMax.Y), new Vector2(plotMax.X, plotMax.Y), Paint.Col(Styling.WithAlpha(Styling.BorderDim, 0.7f)), 1f);

        var gap = 2f * scale;
        var barWidth = MathF.Max(2f * scale, (plotWidth - gap * (count - 1)) / count);
        var stride = barWidth + gap;
        var hovered = -1;
        if (Hit.HoveringRect(plotMin, plotMax))
        {
            hovered = (int)((ImGui.GetMousePos().X - plotMin.X) / stride);
            if (hovered >= count) hovered = -1;
        }

        var rounding = MathF.Min(4f * scale, barWidth * 0.5f);
        for (var index = 0; index < count; index++)
        {
            var record = records[count - 1 - index];
            var height = MathF.Max(2f * scale, plotHeight * record.MatchesCompleted / peak);
            var barMin = new Vector2(plotMin.X + stride * index, plotMax.Y - height);
            var barMax = new Vector2(barMin.X + barWidth, plotMax.Y);
            var color = index == hovered ? Styling.AccentBlueSoft : Styling.AccentBlue;
            dl.AddRectFilled(barMin, barMax, Paint.Col(color), rounding, ImDrawFlags.RoundCornersTop);
        }

        if (hovered >= 0)
        {
            var record = records[count - 1 - hovered];
            Tooltip.Show(Loc.T(L.History.RowSummary, Formatting.RelativeTime(record.EndedAtUtc), record.MatchesCompleted,
                Formatting.Elapsed(record.Duration), Formatting.Exp(record.SeriesExpGained)));
        }

        ImGui.Dummy(size);
    }

    private static void DrawRow(RunRecord record, int index)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(ImGui.GetContentRegionAvail().X, Layout.HistoryRowHeight * scale);
        if (!ImGui.IsRectVisible(size))
        {
            ImGui.Dummy(size);
            return;
        }

        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();

        ImGui.PushID((nint)(index + 1));
        var hit = Hit.Area("##run", size, handCursor: false);
        var hover = Motion.Hover(Motion.Key("##run"), hit.Hovered);
        ImGui.PopID();

        Paint.Glass(dl, origin, end, Styling.CardRounding * scale, Styling.AccentViolet, 0.02f, hover);

        var padX = PadX * scale;
        var midY = origin.Y + size.Y * 0.5f;
        var when = Formatting.RelativeTime(record.EndedAtUtc);
        var job = string.IsNullOrEmpty(record.JobAbbr) ? "—" : record.JobAbbr;
        var detail = Loc.T(L.History.RowJob, job, Formatting.Elapsed(record.Duration));

        var whenSize = TextDraw.Measure(when);
        Vector2 detailSize;
        using (Fonts.PushCaption())
            detailSize = TextDraw.Measure(detail);
        var top = midY - (whenSize.Y + 3f * scale + detailSize.Y) * 0.5f;
        TextDraw.At(when, new Vector2(origin.X + padX, top), Styling.TextStrong);
        using (Fonts.PushCaption())
            TextDraw.At(detail, new Vector2(origin.X + padX, top + whenSize.Y + 3f * scale), Styling.TextDim);

        var metricWidth = MetricWidth * scale;
        var rate = record.Duration.TotalHours > 0 ? record.MatchesCompleted / record.Duration.TotalHours : 0;
        var x = end.X - padX - metricWidth;
        DrawMetric(x, midY, metricWidth, rate > 0 ? rate.ToString("F1") : "—", Loc.T(L.History.PerHour), rate > 0 ? Styling.AccentMint : Styling.TextMuted);
        x -= metricWidth;
        DrawMetric(x, midY, metricWidth, record.SeriesExpGained > 0 ? $"+{Formatting.Exp(record.SeriesExpGained)}" : "—", Loc.T(L.History.Series), record.SeriesExpGained > 0 ? Styling.AccentAmber : Styling.TextMuted);
        x -= metricWidth;
        DrawMetric(x, midY, metricWidth, record.MatchesCompleted.ToString(), Loc.T(L.History.UnitMatches), Styling.AccentBlue);

        if (hit.Hovered) Tooltip.Show(RunTooltip(record));
    }

    private static void DrawMetric(float x, float midY, float width, string value, string label, Vector4 color)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var valueSize = TextDraw.Measure(value);
        var labelSize = TextDraw.SmallCapsSize(label);
        var top = midY - (valueSize.Y + 2f * scale + labelSize.Y) * 0.5f;
        var centerX = x + width * 0.5f;
        TextDraw.At(value, new Vector2(centerX - valueSize.X * 0.5f, top), color);
        TextDraw.SmallCaps(label, new Vector2(centerX - labelSize.X * 0.5f, top + valueSize.Y + 2f * scale), Styling.TextMuted);
    }

    private static string RunTooltip(RunRecord record)
    {
        var lines = record.EndedAtUtc.ToLocalTime().ToString("g", Loc.Culture);
        if (!string.IsNullOrEmpty(record.JobAbbr)) lines += "\n" + Loc.T(L.History.TooltipJob, record.JobAbbr);
        lines += "\n" + Loc.T(L.History.TooltipMatches, record.MatchesCompleted, Formatting.Elapsed(record.Duration));
        if (record.SeriesExpGained > 0) lines += "\n" + Loc.T(L.History.TooltipExp, Formatting.Exp(record.SeriesExpGained));
        return lines;
    }

    private void DrawClearControl(RunHistory history)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var avail = ImGui.GetContentRegionAvail().X;
        var origin = ImGui.GetCursorScreenPos();
        var reveal = Motion.Transition(Motion.Key("##apsg_hist_clear_state"), confirmClear);
        var slide = (1f - reveal) * ConfirmSlide * scale;
        using var alpha = Motion.PushAlpha(reveal);

        if (!confirmClear)
        {
            var label = Loc.T(L.History.Clear);
            ImGui.SetCursorScreenPos(new Vector2(origin.X + avail - PillButton.Width(label, FontAwesomeIcon.Trash) - slide, origin.Y));
            if (PillButton.Draw("##apsg_hist_clear", label, Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Trash)) confirmClear = true;
            return;
        }

        var question = Loc.T(L.History.ClearConfirm);
        var yes = Loc.T(L.History.ClearYes);
        var no = Loc.T(L.Common.Cancel);
        var questionSize = TextDraw.Measure(question);
        var yesWidth = PillButton.Width(yes);
        var noWidth = PillButton.Width(no);
        var gap = 8f * scale;
        var buttonHeight = 28f * scale;

        var x = origin.X + avail - noWidth - slide;
        ImGui.SetCursorScreenPos(new Vector2(x, origin.Y));
        if (PillButton.Draw("##apsg_hist_clear_no", no, Styling.AccentViolet, PillButton.Emphasis.Ghost)) confirmClear = false;

        x -= gap + yesWidth;
        ImGui.SetCursorScreenPos(new Vector2(x, origin.Y));
        if (PillButton.Draw("##apsg_hist_clear_yes", yes, Styling.AccentRose, PillButton.Emphasis.Filled))
        {
            history.Clear();
            confirmClear = false;
        }

        TextDraw.At(question, new Vector2(x - gap * 1.5f - questionSize.X, origin.Y + (buttonHeight - questionSize.Y) * 0.5f), Styling.AccentRoseSoft);
    }
}
