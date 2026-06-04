using AutoPvpSeriesGrind.Core.Stats;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed class RunHistoryWindow : Window, IDisposable
{
    private bool confirmClear;

    public RunHistoryWindow() : base("Auto PVP Series Grind — Run History###AutoPvpSeriesGrindHistory")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
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

    private static void DrawLifetime(RunHistory.LifetimeTotals t)
    {
        Styling.SectionLabel("Lifetime");
        ImGui.Spacing();

        var s = ImGuiHelpers.GlobalScale;
        var gap = 7f * s;
        var avail = ImGui.GetContentRegionAvail().X;
        var tileW = (avail - gap * 3f) / 4f;
        var size = new Vector2(tileW, 60f * s);

        StatTile(FontAwesomeIcon.Flag, t.Runs.ToString("N0"), "Runs", Styling.AccentViolet, size);
        ImGui.SameLine(0, gap);
        StatTile(FontAwesomeIcon.Trophy, t.Matches.ToString("N0"), "Matches", Styling.AccentBlue, size);
        ImGui.SameLine(0, gap);
        StatTile(FontAwesomeIcon.Skull, t.Deaths.ToString("N0"), "Deaths", Styling.AccentRose, size);
        ImGui.SameLine(0, gap);
        StatTile(FontAwesomeIcon.ChartLine, $"{t.MatchesPerHour:F1}", "Matches/h", Styling.AccentMint, size);

        ImGui.Spacing();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"{Formatting.Elapsed(t.Duration)} grinding");
    }

    private static void StatTile(FontAwesomeIcon icon, string value, string caption, Vector4 accent, Vector2 size)
    {
        var s = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Vector4.Lerp(Styling.CardBg, accent, 0.10f)), Styling.CardRounding);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.BorderDim), Styling.CardRounding, ImDrawFlags.None, 1f);
        dl.AddRectFilled(origin, new Vector2(end.X, origin.Y + 3f * s), ImGui.GetColorU32(accent), Styling.CardRounding, ImDrawFlags.RoundCornersTop);

        var pad = 10f * s;
        var iconStr = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);

        var topY = origin.Y + 9f * s;
        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, topY));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, accent))
            ImGui.TextUnformatted(iconStr);

        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad + iconSize.X + 6f * s, topY + (iconSize.Y - ImGui.GetTextLineHeight()) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(caption);

        ImGui.SetWindowFontScale(1.45f);
        var valSize = ImGui.CalcTextSize(value);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad, end.Y - valSize.Y - 8f * s));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(value);
        ImGui.SetWindowFontScale(1f);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
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
        using var table = ImRaii.Table("##apsg_history", 5, flags, new Vector2(-1, -44 * ImGuiHelpers.GlobalScale));
        if (!table) return;

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("When", ImGuiTableColumnFlags.WidthStretch, 1.4f);
        ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthStretch, 0.7f);
        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthStretch, 0.9f);
        ImGui.TableSetupColumn("Matches", ImGuiTableColumnFlags.WidthStretch, 0.8f);
        ImGui.TableSetupColumn("Deaths", ImGuiTableColumnFlags.WidthStretch, 0.7f);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        HeaderCell(FontAwesomeIcon.Clock, "When");
        HeaderCell(FontAwesomeIcon.User, "Job");
        HeaderCell(FontAwesomeIcon.Stopwatch, "Time");
        HeaderCell(FontAwesomeIcon.Trophy, "Matches");
        HeaderCell(FontAwesomeIcon.Skull, "Deaths");

        var i = 0;
        foreach (var r in history.Records)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Selectable($"##apsg_run{i++}", false, ImGuiSelectableFlags.SpanAllColumns);
            ImGui.SameLine(0, 0);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
                ImGui.TextUnformatted(RelativeTime(r.EndedAtUtc));

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, string.IsNullOrEmpty(r.JobAbbr) ? Styling.TextMuted : Styling.TextSecondary))
                ImGui.TextUnformatted(string.IsNullOrEmpty(r.JobAbbr) ? "—" : r.JobAbbr);

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
                ImGui.TextUnformatted(Formatting.Elapsed(r.Duration));

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentBlue))
                ImGui.TextUnformatted(r.MatchesCompleted.ToString());

            ImGui.TableNextColumn();
            using (ImRaii.PushColor(ImGuiCol.Text, r.Deaths > 0 ? Styling.AccentRose : Styling.TextMuted))
                ImGui.TextUnformatted(r.Deaths > 0 ? r.Deaths.ToString() : "—");
        }
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

    private static string RelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalSeconds < 60) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return utc.ToLocalTime().ToString("MMM d");
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
