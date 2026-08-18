using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed partial class AboutWindow
{
    private static void DrawSupport()
    {
        var s = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var pulse = Styling.Pulse(Styling.PulseBreath);
        var accent = Styling.PulseColor(Styling.AccentPink, Styling.AccentViolet, 5200.0);

        const string title = "Made with care";
        const string body = "I build and maintain this in my spare time. If it has helped you, a Patreon membership lets me keep improving it. No pressure, and thank you for being here.";

        var slotOrigin = ImGui.GetCursorScreenPos();
        var fullAvail = ImGui.GetContentRegionAvail().X;
        var margin = 24f * s;
        var origin = new Vector2(slotOrigin.X + margin, slotOrigin.Y);
        var availX = fullAvail - margin * 2f;
        var pad = 16f * s;
        var medR = 22f * s;
        var btnH = 36f * s;
        var innerW = availX - pad * 2f;
        var lineH = ImGui.GetTextLineHeight();
        var spacing = ImGui.GetStyle().ItemSpacing.Y;
        var titleH = lineH * 1.12f;

        var bodyLines = WrapLines(body, innerW);
        var bodyBlockH = bodyLines.Count * lineH + MathF.Max(0, bodyLines.Count - 1) * spacing;
        var height = pad + medR * 2f + 12f * s + titleH + spacing + bodyBlockH + 14f * s + btnH + pad;

        var end = new Vector2(origin.X + availX, origin.Y + height);
        var centerX = origin.X + availX * 0.5f;

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Vector4.Lerp(Styling.CardBg, Styling.AccentPink, 0.07f)), Styling.CardRounding);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.WithAlpha(accent, 0.55f + 0.35f * pulse)),
            Styling.CardRounding, ImDrawFlags.None, 1.5f);

        var beat = Heartbeat(1400.0);
        var medC = new Vector2(centerX, origin.Y + pad + medR);
        ProgressRing.Glow(medC, medR, accent, 0.4f + 0.7f * beat);
        dl.AddCircleFilled(medC, medR, ImGui.GetColorU32(Vector4.Lerp(Styling.CardBg, accent, 0.28f)));
        ProgressRing.Track(medC, medR, 1.5f * s, Styling.WithAlpha(accent, 0.85f));
        ProgressRing.CenterIcon(medC, FontAwesomeIcon.Heart, Lighten(accent, 0.25f), medR * (0.80f + 0.22f * beat));

        ImGui.SetCursorScreenPos(new Vector2(slotOrigin.X, origin.Y + pad + medR * 2f + 12f * s));
        Styling.TextCentered(title, Styling.TextStrong, 1.12f);
        for (var lineIndex = 0; lineIndex < bodyLines.Count; lineIndex++)
        {
            Styling.TextCentered(bodyLines[lineIndex], Styling.TextSecondary);
        }

        var btnOrigin = new Vector2(origin.X + pad, end.Y - pad - btnH);
        var btnSize = new Vector2(innerW, btnH);
        PatreonButton(btnOrigin, btnSize, accent);

        ImGui.SetCursorScreenPos(slotOrigin);
        ImGui.Dummy(new Vector2(fullAvail, height));
    }

    private static List<string> WrapLines(string text, float maxWidth)
    {
        var lines = new List<string>();
        var currentLine = "";
        var words = text.Split(' ');
        for (var wordIndex = 0; wordIndex < words.Length; wordIndex++)
        {
            var word = words[wordIndex];
            var candidate = currentLine.Length == 0 ? word : currentLine + " " + word;
            if (currentLine.Length > 0 && ImGui.CalcTextSize(candidate).X > maxWidth)
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = candidate;
            }
        }
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine);
        }
        return lines;
    }

    private static void PatreonButton(Vector2 origin, Vector2 size, Vector4 accent)
    {
        var s = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();
        var end = origin + size;
        var hover = ImGui.IsMouseHoveringRect(origin, end);
        var rounding = size.Y * 0.5f;

        var fill = (hover ? Lighten(accent, 0.16f) : accent) with { W = 1f };

        var glowPulse = 0.5f + 0.5f * Styling.Pulse(Styling.PulseBreath);
        for (var i = 3; i >= 1; i--)
        {
            var grow = i * 2.6f * s;
            var a = 0.06f * i * glowPulse * (hover ? 1.8f : 1f);
            drawList.AddRectFilled(origin - new Vector2(grow, grow), end + new Vector2(grow, grow),
                ImGui.GetColorU32(Styling.WithAlpha(fill, a)), rounding + grow);
        }

        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(fill), rounding);
        drawList.AddLine(new Vector2(origin.X + rounding, origin.Y + 1.5f * s), new Vector2(end.X - rounding, origin.Y + 1.5f * s),
            ImGui.GetColorU32(Styling.WithAlpha(Styling.White, 0.22f)), 1f);
        Sheen(origin, size, 3000.0);
        drawList.AddRect(origin, end, ImGui.GetColorU32(Styling.WithAlpha(Styling.White, hover ? 0.42f : 0.18f)),
            rounding, ImDrawFlags.None, 1f);

        const string label = "Support on Patreon";
        var iconStr = FontAwesomeIcon.HandHoldingHeart.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);
        var labelSize = ImGui.CalcTextSize(label);
        var innerGap = 9f * s;
        var contentW = iconSize.X + innerGap + labelSize.X;
        var startX = origin.X + (size.X - contentW) * 0.5f;
        var midY = origin.Y + size.Y * 0.5f;
        var breathe = Styling.Pulse(2200.0);

        ImGui.SetWindowFontScale(1f + 0.09f * breathe);
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            var hs = ImGui.CalcTextSize(iconStr);
            ImGui.SetCursorScreenPos(new Vector2(startX, midY - hs.Y * 0.5f));
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
                ImGui.TextUnformatted(iconStr);
        }
        ImGui.SetWindowFontScale(1f);
        ImGui.SetCursorScreenPos(new Vector2(startX + iconSize.X + innerGap, midY - labelSize.Y * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(label);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);

        if (!hover) return;
        UrlActions.HoveredLinkInteraction(PatreonUrl, "Open Patreon · right-click to copy");
    }

    private static void Sheen(Vector2 origin, Vector2 size, double periodMs)
    {
        var p = Styling.Phase(periodMs);
        if (p > 0.35f) return;
        var sweep = p / 0.35f;

        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(origin, origin + size, true);
        var slant = size.Y * 0.55f;
        var travel = size.X + slant + 40f;
        var cx = origin.X - 20f + sweep * travel;
        const int half = 15;
        for (var k = -half; k <= half; k++)
        {
            var a = 0.16f * (1f - MathF.Abs(k) / (float)half);
            var x = cx + k;
            drawList.AddLine(new Vector2(x + slant, origin.Y), new Vector2(x, origin.Y + size.Y),
                ImGui.GetColorU32(Styling.WithAlpha(Styling.White, a)), 1.3f);
        }
        drawList.PopClipRect();
    }
}
