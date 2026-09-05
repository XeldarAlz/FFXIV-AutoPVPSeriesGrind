using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed partial class AboutPage
{
    private const float IconSize = 148f;
    private const float RingRadius = 120f;

    private static void DrawHero()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();

        Styling.VSpace(32);

        var start = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;
        var bob = Motion.Wave(3000) * 3f * scale;
        var center = new Vector2(start.X + avail * 0.5f, start.Y + RingRadius * scale + bob);

        ProgressRing.Glow(center, RingRadius * scale, Styling.AccentViolet, 0.55f + 0.5f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, RingRadius * scale, 1.5f * scale, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Sweep(center, RingRadius * scale, 2.6f * scale, Styling.AccentVioletSoft, Styling.PulseOrbit, MathF.PI * 0.55f, 1f);
        OrbitParticles(center, RingRadius * scale, 3, 4600, +1, Styling.AccentVioletSoft, 2.4f * scale);
        OrbitParticles(center, RingRadius * scale * 0.74f, 2, 6000, -1, Styling.AccentPink, 2.0f * scale);

        var half = IconSize * 0.5f * scale;
        var iconMin = new Vector2(center.X - half, center.Y - half);
        var iconMax = new Vector2(center.X + half, center.Y + half);
        var rounding = IconSize * 0.20f * scale;

        AppIcon.Draw(dl, iconMin, iconMax, rounding, 0.92f + 0.08f * Styling.Pulse(2200.0));
        Paint.Stroke(dl, iconMin, iconMax, Styling.WithAlpha(Styling.AccentVioletSoft, 0.55f), rounding, 1.5f * scale);

        IconEasterEgg(iconMin, iconMax);

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(avail, RingRadius * 2f * scale));

        Styling.VSpace(10);
        ShimmerCentered(Name, Styling.TextStrong, Styling.AccentVioletSoft, Styling.PulseOrbit, 0.42f);
        Styling.VSpace(9);

        var version = typeof(AboutPage).Assembly.GetName().Version?.ToString() ?? "?";
        CenteredPill($"v {version}", Styling.TextSecondary,
            Styling.WithAlpha(Styling.AccentViolet, 0.45f), Styling.CardBgSoft);
    }

    private static void OrbitParticles(Vector2 center, float radius, int count, double periodMs, int direction, Vector4 color, float dotRadius)
    {
        var dl = ImGui.GetWindowDrawList();
        var baseAngle = -MathF.PI / 2f + direction * Styling.Phase(periodMs) * MathF.PI * 2f;
        for (var index = 0; index < count; index++)
        {
            var angle = baseAngle + index * (MathF.PI * 2f / count);
            var point = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            dl.AddCircleFilled(point, dotRadius * 2.4f, Paint.Col(Styling.WithAlpha(color, 0.16f)));
            dl.AddCircleFilled(point, dotRadius * 1.5f, Paint.Col(Styling.WithAlpha(color, 0.32f)));
            dl.AddCircleFilled(point, dotRadius, Paint.Col(color));
        }
    }

    private static void ShimmerCentered(string text, Vector4 baseColor, Vector4 shimmerColor, double periodMs, float bandFraction)
    {
        using var font = Fonts.PushTitle();
        var size = TextDraw.Measure(text);
        var avail = ImGui.GetContentRegionAvail().X;
        var cursor = ImGui.GetCursorScreenPos();
        var origin = cursor with { X = cursor.X + MathF.Max(0f, (avail - size.X) * 0.5f) };

        var bloom = Styling.WithAlpha(Styling.AccentViolet, 0.22f);
        for (var offsetIndex = 0; offsetIndex < BloomOffsets.Length; offsetIndex++)
        {
            TextDraw.At(text, origin + BloomOffsets[offsetIndex] * ImGuiHelpers.GlobalScale, bloom);
        }

        TextDraw.At(text, origin, baseColor);

        var dl = ImGui.GetWindowDrawList();
        var bandWidth = size.X * bandFraction;
        var phase = Styling.Phase(periodMs);
        var bandCenter = origin.X - bandWidth + phase * (size.X + bandWidth * 2f);

        dl.PushClipRect(
            new Vector2(bandCenter - bandWidth * 0.5f, origin.Y),
            new Vector2(bandCenter + bandWidth * 0.5f, origin.Y + size.Y),
            true);
        TextDraw.At(text, origin, shimmerColor);
        dl.PopClipRect();

        ImGui.Dummy(new Vector2(avail, size.Y));
    }

    private static void CenteredPill(string text, Vector4 textColor, Vector4 borderColor, Vector4 backgroundColor)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var font = Fonts.PushCaption();
        var padX = 11f * scale;
        var padY = 4f * scale;
        var textSize = TextDraw.Measure(text);
        var size = new Vector2(textSize.X + padX * 2f, textSize.Y + padY * 2f);

        Styling.CenterNextItem(size.X);
        var origin = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();
        Paint.Pill(dl, origin, origin + size, backgroundColor, borderColor);
        TextDraw.At(text, new Vector2(origin.X + padX, origin.Y + padY), textColor);

        ImGui.Dummy(size);
    }
}
