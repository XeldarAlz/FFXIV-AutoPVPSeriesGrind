using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed partial class AboutPage
{
    private static readonly Vector4[] ConnectAccents =
    {
        Styling.AccentViolet, Styling.AccentBlue, Styling.AccentRose,
        Styling.AccentMint, Styling.AccentAmber, Styling.AccentDiscord,
    };

    // Sized by a constant rather than Links.Length: static initializers across partial class files
    // run in an unspecified file order, so reading Links here can see it before it is assigned.
    private const int MaxLinks = 12;

    private static readonly float[] linkWidths = new float[MaxLinks];
    private static readonly int[] rowStarts = new int[MaxLinks + 1];

    private static void DrawConnect()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = 7f * scale;
        var avail = ImGui.GetContentRegionAvail().X;
        var pillHeight = ImGui.GetFrameHeight() * 1.15f;

        var count = Math.Min(Links.Length, MaxLinks);
        for (var index = 0; index < count; index++)
        {
            linkWidths[index] = PillWidth(Links[index].Icon, Loc.T(Links[index].Label));
        }

        var rowCount = ComputeFlowRows(count, gap, avail);
        for (var row = 0; row < rowCount; row++)
        {
            var first = rowStarts[row];
            var last = rowStarts[row + 1];
            var rowWidth = gap * (last - first - 1);
            for (var index = first; index < last; index++) rowWidth += linkWidths[index];

            var startX = ImGui.GetCursorPosX() + MathF.Max(0f, (avail - rowWidth) * 0.5f);
            for (var index = first; index < last; index++)
            {
                if (index == first) ImGui.SetCursorPosX(startX);
                else ImGui.SameLine(0, gap);

                var (icon, label, url, accentId) = Links[index];
                LinkPill(icon, Loc.T(label), url, ConnectAccents[accentId % ConnectAccents.Length], new Vector2(linkWidths[index], pillHeight));
            }
        }
    }

    // Fills rowStarts with the index each row begins at, plus a terminating entry, and returns the
    // row count, so the flow layout stays allocation free every frame.
    private static int ComputeFlowRows(int count, float gap, float availableWidth)
    {
        var rowCount = 0;
        rowStarts[0] = 0;
        var rowWidth = 0f;
        var rowMembers = 0;

        for (var index = 0; index < count; index++)
        {
            var candidate = rowMembers == 0 ? linkWidths[index] : rowWidth + gap + linkWidths[index];
            if (rowMembers > 0 && candidate > availableWidth)
            {
                rowCount++;
                rowStarts[rowCount] = index;
                rowWidth = linkWidths[index];
                rowMembers = 1;
                continue;
            }

            rowWidth = candidate;
            rowMembers++;
        }

        rowCount++;
        rowStarts[rowCount] = count;
        return rowCount;
    }

    private static float PillWidth(FontAwesomeIcon icon, string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        return TextDraw.IconSize(icon).X + 6f * scale + TextDraw.Measure(label).X + 14f * scale * 2f;
    }

    private static void LinkPill(FontAwesomeIcon icon, string label, string url, Vector4 accent, Vector2 size)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var slotOrigin = ImGui.GetCursorScreenPos();
        var hit = Hit.Area(url, size);
        var hover = Motion.Hover(Motion.Key(url), hit.Hovered);

        var lift = hover * 2.5f * scale;
        var origin = slotOrigin - new Vector2(0f, lift);
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var rounding = size.Y * 0.5f;

        if (hover > 0.01f)
        {
            for (var layer = 2; layer >= 1; layer--)
            {
                var grow = layer * 2.4f * scale;
                dl.AddRectFilled(origin - new Vector2(grow, grow), end + new Vector2(grow, grow),
                    Paint.Col(Styling.WithAlpha(accent, 0.05f * layer * hover)), rounding + grow);
            }
        }

        var background = Vector4.Lerp(Styling.WithAlpha(Styling.Surface1, 0.7f), Vector4.Lerp(Styling.Surface1, accent, 0.24f), hover);
        var border = Vector4.Lerp(Styling.WithAlpha(Styling.BorderDim, 0.7f), accent, hover);
        Paint.Pill(dl, origin, end, background, border);

        var iconSize = TextDraw.IconSize(icon);
        var labelSize = TextDraw.Measure(label);
        var innerGap = 6f * scale;
        var contentWidth = iconSize.X + innerGap + labelSize.X;
        var startX = origin.X + (size.X - contentWidth) * 0.5f;
        var midY = origin.Y + size.Y * 0.5f;

        TextDraw.Icon(icon, new Vector2(startX, midY - iconSize.Y * 0.5f), Vector4.Lerp(accent, Styling.TextStrong, hover));
        TextDraw.At(label, new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f),
            Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, hover));

        if (!hit.Hovered) return;
        Tooltip.Show(Loc.T(L.About.LinkHint));
        if (hit.Clicked) UrlActions.OpenOrCopy(url);
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) ImGui.SetClipboardText(url);
    }

    private static void SectionHeader(FontAwesomeIcon icon, string label, Vector4 accent)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var font = Fonts.PushCaption();
        var upper = TextDraw.Upper(label);
        var iconSize = TextDraw.IconSize(icon);
        var labelSize = TextDraw.Measure(upper);

        var iconGap = 8f * scale;
        var sidePad = 12f * scale;
        var contentWidth = iconSize.X + iconGap + labelSize.X;

        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var leftX = origin.X;
        var rightX = origin.X + avail;
        var contentStartX = origin.X + MathF.Max(0f, (avail - contentWidth) * 0.5f);
        var lineY = origin.Y + iconSize.Y * 0.5f;

        TextDraw.Icon(icon, new Vector2(contentStartX, origin.Y), accent);
        var labelX = contentStartX + iconSize.X + iconGap;
        TextDraw.At(upper, new Vector2(labelX, origin.Y + (iconSize.Y - labelSize.Y) * 0.5f), Styling.TextDim);

        RuleLine(leftX, contentStartX - sidePad, lineY, accent, brightAtStart: false);
        RuleLine(labelX + labelSize.X + sidePad, rightX, lineY, accent, brightAtStart: true);

        ImGui.Dummy(new Vector2(avail, iconSize.Y));
    }

    private static void RuleLine(float x0, float x1, float y, Vector4 accent, bool brightAtStart)
    {
        if (x1 - x0 < 1f) return;
        var dl = ImGui.GetWindowDrawList();
        var glowPhase = Styling.Phase(3200.0);
        const int segments = 22;
        for (var segment = 0; segment < segments; segment++)
        {
            var t0 = segment / (float)segments;
            var t1 = (segment + 1) / (float)segments;
            var edge = brightAtStart ? t0 : 1f - t0;
            var fade = 0.5f * (1f - edge);
            var travel = MathF.Max(0f, 1f - MathF.Abs(t0 - glowPhase) * 6f);
            var alpha = fade + 0.35f * travel;
            dl.AddLine(
                new Vector2(x0 + (x1 - x0) * t0, y),
                new Vector2(x0 + (x1 - x0) * t1, y),
                Paint.Col(Styling.WithAlpha(accent, alpha)), 1f);
        }
    }
}
