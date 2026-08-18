using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed partial class AboutWindow
{
    private static readonly Vector4[] ConnectAccents =
    {
        Styling.AccentViolet, Styling.AccentBlue, Styling.AccentRose,
        Styling.AccentMint, Styling.AccentAmber, Styling.AccentDiscord,
    };

    private readonly Dictionary<string, float> pillHoverProgress = new();

    private void DrawConnect()
    {
        var s = ImGuiHelpers.GlobalScale;
        var gap = 7f * s;
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var pillHeight = ImGui.GetFrameHeight() * 1.15f;

        var widths = new float[Links.Length];
        for (var i = 0; i < Links.Length; i++)
            widths[i] = PillWidth(Links[i].Icon, Links[i].Label);

        var rows = ComputeFlowRows(widths, gap, availableWidth);

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var rowWidth = gap * (row.Count - 1);
            for (var memberIndex = 0; memberIndex < row.Count; memberIndex++)
            {
                rowWidth += widths[row[memberIndex]];
            }

            var startX = ImGui.GetCursorPosX() + MathF.Max(0f, (availableWidth - rowWidth) * 0.5f);
            for (var pillIndex = 0; pillIndex < row.Count; pillIndex++)
            {
                if (pillIndex == 0)
                {
                    ImGui.SetCursorPosX(startX);
                }
                else
                {
                    ImGui.SameLine(0, gap);
                }
                var (icon, label, url, accentId) = Links[row[pillIndex]];
                LinkPill(icon, label, url, ConnectAccents[accentId % ConnectAccents.Length], new Vector2(widths[row[pillIndex]], pillHeight));
            }
        }
    }

    private static List<List<int>> ComputeFlowRows(float[] widths, float gap, float availableWidth)
    {
        var rows = new List<List<int>>();
        var currentRow = new List<int>();
        var currentRowWidth = 0f;
        for (var linkIndex = 0; linkIndex < widths.Length; linkIndex++)
        {
            var widthIncludingNext = currentRow.Count == 0 ? widths[linkIndex] : currentRowWidth + gap + widths[linkIndex];
            if (currentRow.Count > 0 && widthIncludingNext > availableWidth)
            {
                rows.Add(currentRow);
                currentRow = new List<int>();
                currentRowWidth = 0f;
            }
            currentRowWidth = currentRow.Count == 0 ? widths[linkIndex] : currentRowWidth + gap + widths[linkIndex];
            currentRow.Add(linkIndex);
        }
        if (currentRow.Count > 0)
        {
            rows.Add(currentRow);
        }
        return rows;
    }

    private static float PillWidth(FontAwesomeIcon icon, string label)
    {
        var s = ImGuiHelpers.GlobalScale;
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(icon.ToIconString());
        var labelSize = ImGui.CalcTextSize(label);
        return iconSize.X + 6f * s + labelSize.X + 14f * s * 2f;
    }

    private void LinkPill(FontAwesomeIcon icon, string label, string url, Vector4 accent, Vector2 size)
    {
        var s = ImGuiHelpers.GlobalScale;
        var slotOrigin = ImGui.GetCursorScreenPos();
        var hovered = ImGui.IsMouseHoveringRect(slotOrigin, slotOrigin + size);

        pillHoverProgress.TryGetValue(url, out var hoverProgress);
        var deltaTime = ImGui.GetIO().DeltaTime;
        hoverProgress += ((hovered ? 1f : 0f) - hoverProgress) * (1f - MathF.Exp(-14f * deltaTime));
        if (hoverProgress < 0.001f) hoverProgress = 0f;
        pillHoverProgress[url] = hoverProgress;

        var lift = hoverProgress * 2.5f * s;
        var origin = slotOrigin - new Vector2(0, lift);
        var end = origin + size;
        var drawList = ImGui.GetWindowDrawList();
        var rounding = size.Y * 0.5f;

        if (hoverProgress > 0.01f)
            for (var i = 2; i >= 1; i--)
            {
                var grow = i * 2.4f * s;
                drawList.AddRectFilled(origin - new Vector2(grow, grow), end + new Vector2(grow, grow),
                    ImGui.GetColorU32(Styling.WithAlpha(accent, 0.05f * i * hoverProgress)), rounding + grow);
            }

        var bg = Vector4.Lerp(Styling.CardBgSoft, Vector4.Lerp(Styling.CardBg, accent, 0.24f), hoverProgress);
        var border = Vector4.Lerp(Styling.BorderDim, accent, hoverProgress);
        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(bg), rounding);
        drawList.AddRect(origin, end, ImGui.GetColorU32(border), rounding, ImDrawFlags.None, 1f);

        var iconStr = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);
        var labelSize = ImGui.CalcTextSize(label);
        var innerGap = 6f * s;
        var contentW = iconSize.X + innerGap + labelSize.X;
        var startX = origin.X + (size.X - contentW) * 0.5f;
        var midY = origin.Y + size.Y * 0.5f;

        ImGui.SetCursorScreenPos(new Vector2(startX, midY - iconSize.Y * 0.5f));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, Vector4.Lerp(accent, Styling.TextStrong, hoverProgress)))
            ImGui.TextUnformatted(iconStr);
        ImGui.SetCursorScreenPos(new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, hoverProgress)))
            ImGui.TextUnformatted(label);

        ImGui.SetCursorScreenPos(slotOrigin);
        ImGui.Dummy(size);

        if (!hovered) return;
        UrlActions.HoveredLinkInteraction(url, "Click to open · right-click to copy");
    }

    private static void SectionHeader(FontAwesomeIcon icon, string label, Vector4 accent)
    {
        var s = ImGuiHelpers.GlobalScale;
        var iconStr = icon.ToIconString();
        var labelUp = label.ToUpperInvariant();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);
        var labelSize = ImGui.CalcTextSize(labelUp);

        var iconGap = 8f * s;
        var sidePad = 12f * s;
        var contentW = iconSize.X + iconGap + labelSize.X;

        var startScreen = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var leftX = startScreen.X;
        var rightX = startScreen.X + avail;
        var contentStartX = startScreen.X + MathF.Max(0f, (avail - contentW) * 0.5f);
        var lineY = startScreen.Y + iconSize.Y * 0.5f;

        ImGui.SetCursorScreenPos(new Vector2(contentStartX, startScreen.Y));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, accent))
            ImGui.TextUnformatted(iconStr);
        var labelX = contentStartX + iconSize.X + iconGap;
        ImGui.SetCursorScreenPos(new Vector2(labelX, startScreen.Y + (iconSize.Y - labelSize.Y) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(labelUp);

        RuleLine(leftX, contentStartX - sidePad, lineY, accent, brightAtStart: false);
        RuleLine(labelX + labelSize.X + sidePad, rightX, lineY, accent, brightAtStart: true);

        ImGui.SetCursorScreenPos(startScreen);
        ImGui.Dummy(new Vector2(avail, iconSize.Y));
    }

    private static void RuleLine(float x0, float x1, float y, Vector4 accent, bool brightAtStart)
    {
        if (x1 - x0 < 1f) return;
        var dl = ImGui.GetWindowDrawList();
        var glowPhase = Styling.Phase(3200.0);
        const int seg = 22;
        for (var i = 0; i < seg; i++)
        {
            var t0 = i / (float)seg;
            var t1 = (i + 1) / (float)seg;
            var edge = brightAtStart ? t0 : 1f - t0;
            var fade = 0.5f * (1f - edge);
            var travel = MathF.Max(0f, 1f - MathF.Abs(t0 - glowPhase) * 6f);
            var a = fade + 0.35f * travel;
            dl.AddLine(
                new Vector2(x0 + (x1 - x0) * t0, y),
                new Vector2(x0 + (x1 - x0) * t1, y),
                ImGui.GetColorU32(Styling.WithAlpha(accent, a)), 1f);
        }
    }
}
