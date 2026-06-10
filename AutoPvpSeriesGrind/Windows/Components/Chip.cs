using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class Chip
{
    private const float FontScale = 0.9f;
    private const float PadX = 8f;
    private const float PadY = 3f;
    private const float DotRadius = 3f;
    private const float DotGap = 6f;
    private const float FillTint = 0.20f;
    private const float BorderAlpha = 0.45f;
    private const float DotPulseBase = 0.45f;
    private const float DotPulseRange = 0.55f;

    public static void Draw(string text, Vector4 accent, bool dot = false, bool pulse = false)
    {
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetWindowFontScale(FontScale);
        var textSize = ImGui.CalcTextSize(text);

        var padX = PadX * scale;
        var padY = PadY * scale;
        var dotRadius = dot ? DotRadius * scale : 0f;
        var dotGap = dot ? DotGap * scale : 0f;
        var size = new Vector2(textSize.X + padX * 2f + dotRadius * 2f + dotGap, textSize.Y + padY * 2f);

        var origin = ImGui.GetCursorScreenPos();
        var max = origin + size;
        var rounding = size.Y * 0.5f;
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(origin, max, ImGui.GetColorU32(Vector4.Lerp(Styling.CardBg, accent, FillTint)), rounding);
        dl.AddRect(origin, max, ImGui.GetColorU32(Styling.WithAlpha(accent, BorderAlpha)), rounding);

        var textX = origin.X + padX;
        if (dot)
        {
            var alpha = pulse ? DotPulseBase + DotPulseRange * Styling.Pulse(Styling.PulseCalm) : 1f;
            dl.AddCircleFilled(new Vector2(origin.X + padX + dotRadius, origin.Y + size.Y * 0.5f), dotRadius,
                ImGui.GetColorU32(Styling.WithAlpha(accent, alpha)));
            textX += dotRadius * 2f + dotGap;
        }

        ImGui.SetCursorScreenPos(new Vector2(textX, origin.Y + padY));
        using (ImRaii.PushColor(ImGuiCol.Text, accent))
            ImGui.TextUnformatted(text);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
        ImGui.SetWindowFontScale(1f);
    }
}
