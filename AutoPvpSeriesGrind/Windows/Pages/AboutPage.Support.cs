using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed partial class AboutPage
{
    private const string SupportTitle = "Made with care";
    private const string SupportBody = "I build and maintain this in my spare time. If it has helped you, a Patreon " +
        "membership lets me keep improving it. No pressure, and thank you for being here.";

    private static void DrawSupport()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var pulse = Styling.Pulse(Styling.PulseBreath);
        var accent = Styling.PulseColor(Styling.AccentPink, Styling.AccentViolet, 5200.0);

        var slotOrigin = ImGui.GetCursorScreenPos();
        var fullAvail = ImGui.GetContentRegionAvail().X;
        var margin = 24f * scale;
        var origin = new Vector2(slotOrigin.X + margin, slotOrigin.Y);
        var availX = fullAvail - margin * 2f;
        var pad = 18f * scale;
        var medalRadius = 22f * scale;
        var buttonHeight = 38f * scale;
        var innerWidth = availX - pad * 2f;

        float titleHeight;
        using (Fonts.PushHeadline())
            titleHeight = ImGui.GetTextLineHeight();
        var bodyHeight = TextDraw.MeasureWrapped(SupportBody, innerWidth).Y;
        var height = pad + medalRadius * 2f + 14f * scale + titleHeight + 8f * scale + bodyHeight + 16f * scale + buttonHeight + pad;

        var end = new Vector2(origin.X + availX, origin.Y + height);
        var centerX = origin.X + availX * 0.5f;
        var rounding = Styling.PanelRounding * scale;

        Paint.Fill(dl, origin, end, Vector4.Lerp(Styling.Surface1, Styling.AccentPink, 0.07f) with { W = 0.95f }, rounding);
        Paint.TopLight(dl, origin, end, rounding);
        Paint.Stroke(dl, origin, end, Styling.WithAlpha(accent, 0.55f + 0.35f * pulse), rounding, 1.5f);

        var beat = Heartbeat(1400.0);
        var medalCenter = new Vector2(centerX, origin.Y + pad + medalRadius);
        ProgressRing.Glow(medalCenter, medalRadius, accent, 0.4f + 0.7f * beat);
        ProgressRing.Disc(medalCenter, medalRadius, Vector4.Lerp(Styling.Surface1, accent, 0.28f));
        ProgressRing.Track(medalCenter, medalRadius, 1.5f * scale, Styling.WithAlpha(accent, 0.85f));
        ProgressRing.CenterIcon(medalCenter, FontAwesomeIcon.Heart, Styling.Lighten(accent, 0.25f), medalRadius * (0.80f + 0.22f * beat));

        var y = origin.Y + pad + medalRadius * 2f + 14f * scale;
        using (Fonts.PushHeadline())
        {
            var titleSize = TextDraw.Measure(SupportTitle);
            TextDraw.At(SupportTitle, new Vector2(centerX - titleSize.X * 0.5f, y), Styling.TextStrong);
            y += titleSize.Y + 8f * scale;
        }

        TextDraw.Wrapped(SupportBody, new Vector2(origin.X + pad, y), innerWidth, Styling.TextSecondary);

        var buttonOrigin = new Vector2(origin.X + pad, end.Y - pad - buttonHeight);
        PatreonButton(buttonOrigin, new Vector2(innerWidth, buttonHeight), accent);

        ImGui.SetCursorScreenPos(slotOrigin);
        ImGui.Dummy(new Vector2(fullAvail, height));
    }

    private static void PatreonButton(Vector2 origin, Vector2 size, Vector4 accent)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var end = origin + size;
        var hovered = Hit.HoveringRect(origin, end);
        var rounding = size.Y * 0.5f;

        var fill = (hovered ? Styling.Lighten(accent, 0.16f) : accent) with { W = 1f };

        var glowPulse = 0.5f + 0.5f * Styling.Pulse(Styling.PulseBreath);
        for (var layer = 3; layer >= 1; layer--)
        {
            var grow = layer * 2.6f * scale;
            var alpha = 0.06f * layer * glowPulse * (hovered ? 1.8f : 1f);
            dl.AddRectFilled(origin - new Vector2(grow, grow), end + new Vector2(grow, grow),
                Paint.Col(Styling.WithAlpha(fill, alpha)), rounding + grow);
        }

        Paint.Fill(dl, origin, end, fill, rounding);
        Paint.TopLight(dl, origin, end, rounding, 0.22f);
        Sheen(origin, size, 3000.0);
        Paint.Stroke(dl, origin, end, Styling.WithAlpha(Styling.White, hovered ? 0.42f : 0.18f), rounding);

        const string label = "Support on Patreon";
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.HandHoldingHeart);
        var labelSize = TextDraw.Measure(label);
        var innerGap = 9f * scale;
        var contentWidth = iconSize.X + innerGap + labelSize.X;
        var startX = origin.X + (size.X - contentWidth) * 0.5f;
        var midY = origin.Y + size.Y * 0.5f;

        TextDraw.Icon(FontAwesomeIcon.HandHoldingHeart, new Vector2(startX, midY - iconSize.Y * 0.5f), Styling.TextStrong);
        TextDraw.At(label, new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f), Styling.TextStrong);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);

        if (!hovered) return;
        UrlActions.HoveredLinkInteraction(PatreonUrl, "Open Patreon, right-click to copy the link.");
    }

    private static void Sheen(Vector2 origin, Vector2 size, double periodMs)
    {
        var phase = Styling.Phase(periodMs);
        if (phase > 0.35f) return;
        var sweep = phase / 0.35f;

        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(origin, origin + size, true);
        var slant = size.Y * 0.55f;
        var travel = size.X + slant + 40f;
        var centerX = origin.X - 20f + sweep * travel;
        const int half = 15;
        for (var step = -half; step <= half; step++)
        {
            var alpha = 0.16f * (1f - MathF.Abs(step) / (float)half);
            var x = centerX + step;
            dl.AddLine(new Vector2(x + slant, origin.Y), new Vector2(x, origin.Y + size.Y),
                Paint.Col(Styling.WithAlpha(Styling.White, alpha)), 1.3f);
        }

        dl.PopClipRect();
    }
}
