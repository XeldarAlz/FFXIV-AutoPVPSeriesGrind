using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class ProgressRing
{
    private const float Top = -MathF.PI / 2f;
    private const float ArcSegmentStep = MathF.PI / 48f;
    private const float SweepSegmentStep = MathF.PI / 36f;
    private const float SweepHeadRadiusRatio = 0.62f;
    private const int GlowLayerCount = 4;
    private const float GlowBaseRadiusRatio = 0.72f;
    private const float GlowLayerRadiusStep = 0.17f;
    private const float GlowLayerAlphaStep = 0.05f;
    private const float GlowMaxAlpha = 0.5f;
    private const float MinVisibleFraction = 0.0001f;
    private const float PlayRingThickness = 4.5f;
    private const float PlayIconHeightRatio = 0.78f;
    private const float LockIconHeightRatio = 0.62f;
    private const float PlayGlyphOpticalCenterNudgeRatio = 0.07f;

    private static Vector2 AngleToDirection(float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

    private static void Arc(Vector2 center, float radius, float thickness, float startAngle, float endAngle, uint color)
    {
        var drawList = ImGui.GetWindowDrawList();
        var span = MathF.Abs(endAngle - startAngle);
        var segmentCount = Math.Max(2, (int)MathF.Ceiling(span / ArcSegmentStep));
        var previousPoint = center + AngleToDirection(startAngle) * radius;
        for (var segmentIndex = 1; segmentIndex <= segmentCount; segmentIndex++)
        {
            var angle = startAngle + (endAngle - startAngle) * (segmentIndex / (float)segmentCount);
            var currentPoint = center + AngleToDirection(angle) * radius;
            drawList.AddLine(previousPoint, currentPoint, color, thickness);
            previousPoint = currentPoint;
        }

        var capRadius = thickness * 0.5f;
        drawList.AddCircleFilled(center + AngleToDirection(startAngle) * radius, capRadius, color);
        drawList.AddCircleFilled(center + AngleToDirection(endAngle) * radius, capRadius, color);
    }

    public static void Glow(Vector2 center, float radius, Vector4 color, float intensity)
    {
        var drawList = ImGui.GetWindowDrawList();
        for (var layerIndex = GlowLayerCount; layerIndex >= 1; layerIndex--)
        {
            var layerRadius = radius * (GlowBaseRadiusRatio + layerIndex * GlowLayerRadiusStep);
            var layerAlpha = Math.Clamp(intensity * GlowLayerAlphaStep * (GlowLayerCount + 1 - layerIndex), 0f, GlowMaxAlpha);
            drawList.AddCircleFilled(center, layerRadius, ImGui.GetColorU32(Styling.WithAlpha(color, layerAlpha)));
        }
    }

    public static void Track(Vector2 center, float radius, float thickness, Vector4 color)
        => Arc(center, radius, thickness, Top, Top + MathF.PI * 2f, ImGui.GetColorU32(color));

    public static void Fill(Vector2 center, float radius, float thickness, float fraction, Vector4 color)
    {
        fraction = Math.Clamp(fraction, 0f, 1f);
        if (fraction <= MinVisibleFraction)
        {
            return;
        }

        Arc(center, radius, thickness, Top, Top + fraction * MathF.PI * 2f, ImGui.GetColorU32(color));
    }

    public static void Sweep(Vector2 center, float radius, float thickness, Vector4 color, double periodMs, float arcLength, float headAlpha)
    {
        var drawList = ImGui.GetWindowDrawList();
        var headAngle = Top + Styling.Phase(periodMs) * MathF.PI * 2f;
        var tailAngle = headAngle - arcLength;
        var stepCount = Math.Max(10, (int)MathF.Ceiling(arcLength / SweepSegmentStep));
        var previousPoint = center + AngleToDirection(tailAngle) * radius;
        for (var stepIndex = 1; stepIndex <= stepCount; stepIndex++)
        {
            var stepFraction = stepIndex / (float)stepCount;
            var angle = tailAngle + (headAngle - tailAngle) * stepFraction;
            var currentPoint = center + AngleToDirection(angle) * radius;
            drawList.AddLine(previousPoint, currentPoint, ImGui.GetColorU32(Styling.WithAlpha(color, headAlpha * stepFraction * stepFraction)), thickness);
            previousPoint = currentPoint;
        }

        drawList.AddCircleFilled(center + AngleToDirection(headAngle) * radius, thickness * SweepHeadRadiusRatio, ImGui.GetColorU32(Styling.WithAlpha(color, headAlpha)));
    }

    public static void CenterValue(Vector2 center, string big, string? small, Vector4 bigColor, Vector4 smallColor, float bigScale)
    {
        ImGui.SetWindowFontScale(bigScale);
        var bigSize = ImGui.CalcTextSize(big);
        ImGui.SetWindowFontScale(1f);

        var hasSmall = !string.IsNullOrEmpty(small);
        var smallSize = hasSmall ? ImGui.CalcTextSize(small) : Vector2.Zero;
        var gap = hasSmall ? 1f * ImGuiHelpers.GlobalScale : 0f;
        var top = center.Y - (bigSize.Y + gap + smallSize.Y) * 0.5f;

        ImGui.SetCursorScreenPos(new Vector2(center.X - bigSize.X * 0.5f, top));
        ImGui.SetWindowFontScale(bigScale);
        using (ImRaii.PushColor(ImGuiCol.Text, bigColor))
        {
            ImGui.TextUnformatted(big);
        }

        ImGui.SetWindowFontScale(1f);

        if (hasSmall)
        {
            ImGui.SetCursorScreenPos(new Vector2(center.X - smallSize.X * 0.5f, top + bigSize.Y + gap));
            using (ImRaii.PushColor(ImGuiCol.Text, smallColor))
            {
                ImGui.TextUnformatted(small!);
            }
        }
    }

    public static void CenterIcon(Vector2 center, FontAwesomeIcon icon, Vector4 color, float targetHeight)
    {
        var glyph = Glyph.Of(icon);
        float baseHeight;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            baseHeight = ImGui.CalcTextSize(glyph).Y;
        }

        var fontScale = baseHeight > 0 ? targetHeight / baseHeight : 1f;

        ImGui.SetWindowFontScale(fontScale);
        Vector2 scaledSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            scaledSize = ImGui.CalcTextSize(glyph);
        }

        ImGui.SetCursorScreenPos(new Vector2(center.X - scaledSize.X * 0.5f, center.Y - scaledSize.Y * 0.5f));
        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, color))
        {
            ImGui.TextUnformatted(glyph);
        }

        ImGui.SetWindowFontScale(1f);
    }

    public static bool PlayButton(Vector2 center, float radius, bool enabled)
    {
        var drawList = ImGui.GetWindowDrawList();
        var boundsMin = center - new Vector2(radius, radius);
        var boundsMax = center + new Vector2(radius, radius);
        var hovered = enabled && ImGui.IsMouseHoveringRect(boundsMin, boundsMax);

        var accent = Styling.AccentViolet;
        var thickness = PlayRingThickness * ImGuiHelpers.GlobalScale;

        if (enabled)
        {
            Glow(center, radius, accent, 0.85f + (hovered ? 1.0f : 0f) + 0.55f * Styling.Pulse(Styling.PulseBreath));
        }

        drawList.AddCircleFilled(center, radius - thickness * 0.5f, ImGui.GetColorU32(enabled
            ? Vector4.Lerp(Styling.CardBg, accent, hovered ? 0.30f : 0.15f)
            : Styling.CardBgSoft));
        Track(center, radius, thickness, enabled ? Styling.WithAlpha(accent, hovered ? 1f : 0.78f) : Layout.RingTrackColor);

        var glyph = enabled ? FontAwesomeIcon.Play : FontAwesomeIcon.Lock;
        var glyphColor = enabled ? (hovered ? Styling.TextStrong : Styling.AccentVioletSoft) : Styling.TextMuted;
        var opticalCenterNudge = enabled ? new Vector2(radius * PlayGlyphOpticalCenterNudgeRatio, 0f) : Vector2.Zero;
        CenterIcon(center + opticalCenterNudge, glyph, glyphColor, radius * (enabled ? PlayIconHeightRatio : LockIconHeightRatio));

        ImGui.SetCursorScreenPos(boundsMin);
        ImGui.Dummy(boundsMax - boundsMin);

        if (!enabled)
        {
            return false;
        }

        if (hovered)
        {
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }

        return hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }
}
