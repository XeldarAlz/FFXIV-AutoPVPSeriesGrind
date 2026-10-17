using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class SettingsRow
{
    private const float RowHeight = 40f;
    private const float HelpIconGap = 7f;
    private const float CaptionPullUp = 7f;
    private const float CaptionBottomGap = 10f;
    private const float BlockBottomGap = 6f;
    private const float NoteTopGap = 6f;
    private const float NoteBottomGap = 8f;

    public const float ToggleHeight = 22f;

    private readonly record struct RowArea(Vector2 Origin, float RightEdge, float MiddleY, bool Hovered)
    {
        public float Width => RightEdge - Origin.X;
    }

    public static void Draw(string label, string? help, float controlWidth, Action drawControl, float controlHeight = 0f, bool showHelp = false)
    {
        var area = BeginRow();
        var scale = ImGuiHelpers.GlobalScale;
        var labelWidth = ImGui.CalcTextSize(label).X + (string.IsNullOrEmpty(help) ? 0f : 28f * scale);
        if (showHelp || labelWidth + (controlWidth + 20f) * scale > area.Width)
        {
            DrawExpandedRow(area, label, help, controlWidth, drawControl, controlHeight, showHelp);
            return;
        }

        DrawTopDivider(area);
        var labelHovered = DrawLabel(area, label);
        var iconHovered = DrawHelpIcon(area, label, help);

        if (!string.IsNullOrEmpty(help) && (labelHovered || iconHovered))
        {
            Tooltip.Show(help);
        }

        DrawControl(area, controlWidth, controlHeight, drawControl);
        EndRow(area);
    }

    private static void DrawExpandedRow(RowArea area, string label, string? help, float controlWidth,
        Action drawControl, float controlHeight, bool showHelp)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var controlPixels = controlWidth * scale;
        var gap = 18f * scale;
        var stacked = area.Width - controlPixels - gap < 260f * scale;
        var textWidth = MathF.Max(1f, stacked ? area.Width : area.Width - controlPixels - gap);
        var textPos = area.Origin + new Vector2(0f, 10f * scale);
        DrawTopDivider(area);
        TextDraw.Wrapped(label, textPos, textWidth, Styling.TextStrong);
        var textHeight = TextDraw.MeasureWrapped(label, textWidth).Y;
        if (showHelp && !string.IsNullOrEmpty(help))
        {
            using (Fonts.PushCaption())
            {
                var helpPos = textPos + new Vector2(0f, textHeight + 4f * scale);
                TextDraw.Wrapped(help, helpPos, textWidth, Styling.TextSecondary);
                textHeight += 4f * scale + TextDraw.MeasureWrapped(help, textWidth).Y;
            }
        }
        else if (!string.IsNullOrEmpty(help) && Hit.HoveringRect(textPos, textPos + new Vector2(textWidth, textHeight)))
            Tooltip.Show(help);

        var resolvedHeight = controlHeight > 0f ? controlHeight * scale : ImGui.GetFrameHeight();
        var controlY = stacked ? textPos.Y + textHeight + 8f * scale
            : textPos.Y + MathF.Max(0f, (textHeight - resolvedHeight) * 0.5f);
        ImGui.SetCursorScreenPos(new Vector2(stacked ? area.Origin.X : area.RightEdge - controlPixels, controlY));
        drawControl();
        var bottom = MathF.Max(textPos.Y + textHeight, controlY + resolvedHeight) + 10f * scale;
        ImGui.SetCursorScreenPos(area.Origin);
        ImGui.Dummy(new Vector2(area.Width, bottom - area.Origin.Y));
    }

    public static void DrawBlock(string label, string? help, Action drawContent)
    {
        Draw(label, help, 0f, static () => { });
        drawContent();
        Styling.VSpace(BlockBottomGap);
    }

    public static void Note(string text, Vector4? color = null)
    {
        Styling.VSpace(NoteTopGap);
        var wrapLocalX = ImGui.GetCursorPosX() + (SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X);
        ImGui.PushTextWrapPos(wrapLocalX);
        using (ImRaii.PushColor(ImGuiCol.Text, color ?? Styling.TextMuted))
        {
            ImGui.TextUnformatted(text);
        }

        ImGui.PopTextWrapPos();
        Styling.VSpace(NoteBottomGap);
    }

    public static void Caption(string text)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var cursor = ImGui.GetCursorScreenPos();
        ImGui.SetCursorScreenPos(cursor with { Y = cursor.Y - CaptionPullUp * scale });

        var wrapLocalX = ImGui.GetCursorPosX() + (SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X);
        using (Fonts.PushCaption())
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.PushTextWrapPos(wrapLocalX);
            ImGui.TextUnformatted(text);
            ImGui.PopTextWrapPos();
        }

        Styling.VSpace(CaptionBottomGap);
    }

    public static void HelpTooltip(string help) => Tooltip.Show(help);

    private static RowArea BeginRow()
    {
        var origin = ImGui.GetCursorScreenPos();
        var rightEdge = SettingsGroup.ContentRightEdge;
        var rowHeight = RowHeight * ImGuiHelpers.GlobalScale;
        var hovered = ImGui.IsMouseHoveringRect(origin, origin + new Vector2(rightEdge - origin.X, rowHeight));
        return new RowArea(origin, rightEdge, origin.Y + rowHeight * 0.5f, hovered);
    }

    private static void DrawTopDivider(RowArea area)
    {
        if (SettingsGroup.RowDrawnInGroup)
        {
            ImGui.GetWindowDrawList().AddLine(area.Origin, area.Origin with { X = area.RightEdge },
                ImGui.GetColorU32(Styling.Hairline), 1f);
        }

        SettingsGroup.RowDrawnInGroup = true;
    }

    private static bool DrawLabel(RowArea area, string label)
    {
        var labelSize = ImGui.CalcTextSize(label);
        ImGui.SetCursorScreenPos(new Vector2(area.Origin.X, area.MiddleY - labelSize.Y * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, area.Hovered ? Styling.TextStrong : Styling.TextSecondary))
        {
            ImGui.TextUnformatted(label);
        }

        return ImGui.IsItemHovered();
    }

    private static bool DrawHelpIcon(RowArea area, string label, string? help)
    {
        if (string.IsNullOrEmpty(help) || !area.Hovered)
        {
            return false;
        }

        var labelWidth = ImGui.CalcTextSize(label).X;
        var iconString = FontAwesomeIcon.InfoCircle.ToIconString();
        using (Fonts.PushIcon())
        {
            var iconSize = ImGui.CalcTextSize(iconString);
            ImGui.SetCursorScreenPos(new Vector2(
                area.Origin.X + labelWidth + HelpIconGap * ImGuiHelpers.GlobalScale,
                area.MiddleY - iconSize.Y * 0.5f));
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.WithAlpha(Styling.TextMuted, 0.9f)))
            {
                ImGui.TextUnformatted(iconString);
            }
        }

        return ImGui.IsItemHovered();
    }

    private static void DrawControl(RowArea area, float controlWidth, float controlHeight, Action drawControl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var resolvedHeight = controlHeight > 0f ? controlHeight * scale : ImGui.GetFrameHeight();
        ImGui.SetCursorScreenPos(new Vector2(area.RightEdge - controlWidth * scale, area.MiddleY - resolvedHeight * 0.5f));
        drawControl();
    }

    private static void EndRow(RowArea area)
    {
        ImGui.SetCursorScreenPos(area.Origin);
        ImGui.Dummy(new Vector2(area.Width, RowHeight * ImGuiHelpers.GlobalScale));
    }
}
