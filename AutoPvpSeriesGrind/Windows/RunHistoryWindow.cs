using AutoPvpSeriesGrind.Core.Stats;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed class RunHistoryWindow : Window, IDisposable
{
    private const float TableBottomReservedHeight = -44f;

    private static readonly List<string> RowSelectableIds = [];

    private bool confirmClear;

    public RunHistoryWindow() : base("Auto PVP Series Grind - Run History###AutoPvpSeriesGrindHistory")
    {
        Size = new Vector2(620, 460);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(500, 320),
            MaximumSize = new Vector2(2000, 1600),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();

        var history = Plugin.Instance.History;
        DrawLifetime(history.Lifetime);
        ImGui.Spacing();
        ImGui.Spacing();

        Styling.SectionLabel(history.Records.Count > 0 ? $"Recent runs  ·  {history.Records.Count}" : "Recent runs");
        if (history.Records.Count == 0)
        {
            ImGui.Spacing();
            DrawEmptyState();
            return;
        }

        ImGui.Spacing();
        DrawRunTable(history);

        ImGui.Spacing();
        DrawClearControl(history);
    }

    private static void DrawLifetime(RunHistory.LifetimeTotals totals)
    {
        Styling.SectionLabel("Lifetime");
        ImGui.Spacing();

        var s = ImGuiHelpers.GlobalScale;
        var gap = 7f * s;
        var avail = ImGui.GetContentRegionAvail().X;
        var tileW = (avail - gap * 3f) / 4f;
        var size = new Vector2(tileW, 60f * s);

        StatTile.Draw(FontAwesomeIcon.Flag, totals.Runs.ToString("N0"), "Runs", Styling.AccentViolet, size);
        ImGui.SameLine(0, gap);
        StatTile.Draw(FontAwesomeIcon.Trophy, totals.Matches.ToString("N0"), "Matches", Styling.AccentBlue, size);
        ImGui.SameLine(0, gap);
        StatTile.Draw(FontAwesomeIcon.Gem, Formatting.Exp(totals.SeriesExp), "Series EXP", Styling.AccentAmber, size);
        ImGui.SameLine(0, gap);
        StatTile.Draw(FontAwesomeIcon.ChartLine, $"{totals.MatchesPerHour:F1}", "Matches/h", Styling.AccentMint, size);

        ImGui.Spacing();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"{Formatting.Elapsed(totals.Duration)} grinding");
    }

    private static void DrawEmptyState()
    {
        var s = ImGuiHelpers.GlobalScale;
        var size = new Vector2(-1, 88f * s);
        using (Components.Card.Begin("##apsg_hist_empty", size, Styling.CardBgSoft, Styling.BorderDim))
        {
            var icon = FontAwesomeIcon.History.ToIconString();
            ImGui.SetWindowFontScale(1.6f);
            Vector2 iconSize;
            using (ImRaii.PushFont(UiBuilder.IconFont))
                iconSize = ImGui.CalcTextSize(icon);
            ImGui.SetCursorPosX((ImGui.GetWindowSize().X - iconSize.X) * 0.5f);
            using (ImRaii.PushFont(UiBuilder.IconFont))
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted(icon);
            ImGui.SetWindowFontScale(1f);

            ImGui.Spacing();
            var msg = "No runs recorded yet. Finish (or stop) a grind and it'll show up here.";
            ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(msg).X) * 0.5f);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextUnformatted(msg);
        }
    }

    private static void DrawRunTable(RunHistory history)
    {
        const ImGuiTableFlags flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH
            | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.PadOuterX;

        using var rowPad = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(8, 5) * ImGuiHelpers.GlobalScale);
        using var table = ImRaii.Table("##apsg_history", 5, flags, new Vector2(-1, TableBottomReservedHeight * ImGuiHelpers.GlobalScale));
        if (!table) return;

        SetupRunTableColumns();
        DrawRunTableHeader();

        for (var recordIndex = 0; recordIndex < history.Records.Count; recordIndex++)
        {
            DrawRunRecordRow(history.Records[recordIndex], recordIndex);
        }
    }

    private static void SetupRunTableColumns()
    {
        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("When", ImGuiTableColumnFlags.WidthStretch, 1.4f);
        ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthStretch, 0.7f);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthStretch, 0.9f);
        ImGui.TableSetupColumn("Matches", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Series EXP", ImGuiTableColumnFlags.WidthStretch, 0.9f);
    }

    private static void DrawRunTableHeader()
    {
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        HeaderCell(FontAwesomeIcon.Clock, "When");
        HeaderCell(FontAwesomeIcon.User, "Job");
        HeaderCell(FontAwesomeIcon.Stopwatch, "Time");
        HeaderCell(FontAwesomeIcon.Trophy, "Matches");
        HeaderCell(FontAwesomeIcon.Gem, "Series EXP");
    }

    private static void DrawRunRecordRow(RunRecord record, int recordIndex)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.Selectable(RowSelectableId(recordIndex), false, ImGuiSelectableFlags.SpanAllColumns);
        ImGui.SameLine(0, 0);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(Formatting.RelativeTime(record.EndedAtUtc));

        ImGui.TableNextColumn();
        using (ImRaii.PushColor(ImGuiCol.Text, string.IsNullOrEmpty(record.JobAbbr) ? Styling.TextMuted : Styling.TextSecondary))
            ImGui.TextUnformatted(string.IsNullOrEmpty(record.JobAbbr) ? "–" : record.JobAbbr);

        ImGui.TableNextColumn();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(Formatting.Elapsed(record.Duration));

        ImGui.TableNextColumn();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentBlue))
            ImGui.TextUnformatted(record.MatchesCompleted.ToString());

        ImGui.TableNextColumn();
        using (ImRaii.PushColor(ImGuiCol.Text, record.SeriesExpGained > 0 ? Styling.AccentAmber : Styling.TextMuted))
            ImGui.TextUnformatted(record.SeriesExpGained > 0 ? $"+{Formatting.Exp(record.SeriesExpGained)}" : "–");
    }

    private static string RowSelectableId(int rowIndex)
    {
        while (RowSelectableIds.Count <= rowIndex)
        {
            RowSelectableIds.Add($"##apsg_run{RowSelectableIds.Count}");
        }
        return RowSelectableIds[rowIndex];
    }

    private static void HeaderCell(FontAwesomeIcon icon, string label)
    {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted(icon.ToIconString());
        ImGui.SameLine(0, 5f * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(label);
    }

    private void DrawClearControl(RunHistory history)
    {
        if (!confirmClear)
        {
            const string label = "Clear history##apsg_hist_clear";
            var w = ImGui.CalcTextSize("Clear history").X + ImGui.GetStyle().FramePadding.X * 2f;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - w);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                if (ImGui.SmallButton(label))
                    confirmClear = true;
            return;
        }

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
            ImGui.TextUnformatted("Delete all recorded runs?");
        ImGui.SameLine();
        if (ImGui.SmallButton("Yes, clear##apsg_hist_clear_yes"))
        {
            history.Clear();
            confirmClear = false;
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Cancel##apsg_hist_clear_no"))
            confirmClear = false;
    }
}
