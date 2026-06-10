using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class ToggleSwitch
{
    private const float TrackWidth = 38f;
    private const float TrackHeight = 20f;
    private const float KnobInset = 4f;
    private const float KnobPaddingX = 3f;

    public static bool Draw(ref bool value)
    {
        var globalScale = ImGuiHelpers.GlobalScale;
        var accent = Styling.AccentViolet;
        var trackWidth = TrackWidth * globalScale;
        var trackHeight = TrackHeight * globalScale;
        var knobRadius = (trackHeight - KnobInset * globalScale) * 0.5f;
        var paddingX = KnobPaddingX * globalScale;

        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(trackWidth, trackHeight);
        var drawList = ImGui.GetWindowDrawList();
        var hovered = ImGui.IsMouseHoveringRect(origin, end);

        var trackColor = value
            ? Vector4.Lerp(accent * 0.7f, accent, hovered ? 0.6f : 0.3f)
            : Vector4.Lerp(Styling.CardBgSoft, Styling.CardBgHover, hovered ? 1f : 0f);
        var knobColor = value ? Styling.TextStrong : Styling.TextSecondary;

        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(trackColor), trackHeight * 0.5f);
        drawList.AddRect(origin, end, ImGui.GetColorU32(value ? accent : Styling.BorderDim), trackHeight * 0.5f);

        var knobX = value ? end.X - knobRadius - paddingX : origin.X + knobRadius + paddingX;
        var knobY = origin.Y + trackHeight * 0.5f;
        drawList.AddCircleFilled(new Vector2(knobX, knobY), knobRadius, ImGui.GetColorU32(knobColor));

        ImGui.Dummy(new Vector2(trackWidth, trackHeight));

        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            value = !value;
            return true;
        }

        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        return false;
    }
}
