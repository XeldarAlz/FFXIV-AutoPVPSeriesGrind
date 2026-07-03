using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using System.IO;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed partial class AboutWindow
{
    private static readonly string VersionText =
        typeof(AboutWindow).Assembly.GetName().Version?.ToString() ?? "?";

    private static readonly string IconImagePath =
        Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName ?? "", "Images", IconFile);

    private static readonly bool IconImageExists = File.Exists(IconImagePath);

    private void DrawHero()
    {
        var s = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();

        Styling.VSpace(32);

        const float iconSize = 148f;
        const float ringR = 120f;
        var start = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var bob = Wave(3000) * 3f * s;
        var center = new Vector2(start.X + availX * 0.5f, start.Y + ringR * s + bob);

        ProgressRing.Glow(center, ringR * s, Styling.AccentViolet, 0.55f + 0.5f * Styling.Pulse(Styling.PulseBreath));
        ProgressRing.Track(center, ringR * s, 1.5f * s, Styling.WithAlpha(Styling.BorderDim, 0.7f));
        ProgressRing.Sweep(center, ringR * s, 2.6f * s, Styling.AccentVioletSoft, Styling.PulseOrbit, MathF.PI * 0.55f, 1f);
        OrbitParticles(center, ringR * s, 3, 4600, +1, Styling.AccentVioletSoft, 2.4f * s);
        OrbitParticles(center, ringR * s * 0.74f, 2, 6000, -1, Styling.AccentPink, 2.0f * s);

        var half = iconSize * 0.5f * s;
        var iconMin = new Vector2(center.X - half, center.Y - half);
        var iconMax = new Vector2(center.X + half, center.Y + half);

        var rounding = iconSize * 0.20f * s;
        if (IconImageExists)
        {
            var texture = Svc.Texture.GetFromFile(IconImagePath).GetWrapOrEmpty();
            if (texture != null)
            {
                var alpha = 0.92f + 0.08f * Styling.Pulse(2200.0);
                drawList.AddImageRounded(texture.Handle, iconMin, iconMax, Vector2.Zero, Vector2.One,
                    ImGui.GetColorU32(Styling.WithAlpha(Styling.White, alpha)), rounding, ImDrawFlags.RoundCornersAll);
            }
        }
        drawList.AddRect(iconMin, iconMax, ImGui.GetColorU32(Styling.WithAlpha(Styling.AccentVioletSoft, 0.55f)),
            rounding, ImDrawFlags.RoundCornersAll, 1.5f * s);

        IconEasterEgg(iconMin, iconMax, s);

        ImGui.SetCursorScreenPos(start);
        ImGui.Dummy(new Vector2(availX, ringR * 2f * s));

        Styling.VSpace(10);
        ShimmerCentered(Name, Styling.TextStrong, Styling.AccentVioletSoft, 1.85f, Styling.PulseOrbit, 0.42f);
        Styling.VSpace(9);

        CenteredPill($"v {VersionText}", Styling.TextSecondary,
            Styling.WithAlpha(Styling.AccentViolet, 0.45f), Styling.CardBgSoft);
    }

    private static void OrbitParticles(Vector2 c, float r, int count, double periodMs, int dir, Vector4 color, float dotR)
    {
        var dl = ImGui.GetWindowDrawList();
        var baseA = -MathF.PI / 2f + dir * Styling.Phase(periodMs) * MathF.PI * 2f;
        for (var i = 0; i < count; i++)
        {
            var a = baseA + i * (MathF.PI * 2f / count);
            var p = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * r;
            dl.AddCircleFilled(p, dotR * 2.4f, ImGui.GetColorU32(Styling.WithAlpha(color, 0.16f)));
            dl.AddCircleFilled(p, dotR * 1.5f, ImGui.GetColorU32(Styling.WithAlpha(color, 0.32f)));
            dl.AddCircleFilled(p, dotR, ImGui.GetColorU32(color));
        }
    }

    private static void ShimmerCentered(string text, Vector4 baseColor, Vector4 shimmerColor,
        float fontScale, double periodMs, float bandFrac)
    {
        ImGui.SetWindowFontScale(fontScale);
        var size = ImGui.CalcTextSize(text);
        var avail = ImGui.GetContentRegionAvail().X;
        if (avail > size.X)
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - size.X) * 0.5f);

        var startScreen = ImGui.GetCursorScreenPos();

        var bloom = Styling.WithAlpha(Styling.AccentViolet, 0.22f);
        for (var offsetIndex = 0; offsetIndex < BloomOffsets.Length; offsetIndex++)
        {
            ImGui.SetCursorScreenPos(startScreen + BloomOffsets[offsetIndex] * ImGuiHelpers.GlobalScale);
            using (ImRaii.PushColor(ImGuiCol.Text, bloom))
                ImGui.TextUnformatted(text);
        }

        ImGui.SetCursorScreenPos(startScreen);
        using (ImRaii.PushColor(ImGuiCol.Text, baseColor))
            ImGui.TextUnformatted(text);

        var dl = ImGui.GetWindowDrawList();
        var bandW = size.X * bandFrac;
        var phase = Styling.Phase(periodMs);
        var bandCenter = startScreen.X - bandW + phase * (size.X + bandW * 2f);

        dl.PushClipRect(
            new Vector2(bandCenter - bandW * 0.5f, startScreen.Y),
            new Vector2(bandCenter + bandW * 0.5f, startScreen.Y + size.Y),
            true);
        ImGui.SetCursorScreenPos(startScreen);
        using (ImRaii.PushColor(ImGuiCol.Text, shimmerColor))
            ImGui.TextUnformatted(text);
        dl.PopClipRect();

        ImGui.SetWindowFontScale(1f);
    }

    private static void CenteredPill(string text, Vector4 textColor, Vector4 borderColor, Vector4 bgColor)
    {
        var s = ImGuiHelpers.GlobalScale;
        var padX = 11f * s;
        var padY = 3f * s;
        var ts = ImGui.CalcTextSize(text);
        var w = ts.X + padX * 2f;
        var h = ts.Y + padY * 2f;

        Styling.CenterNextItem(w);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(w, h);
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(bgColor), h * 0.5f);
        dl.AddRect(origin, end, ImGui.GetColorU32(borderColor), h * 0.5f, ImDrawFlags.None, 1f);

        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, origin.Y + padY));
        using (ImRaii.PushColor(ImGuiCol.Text, textColor))
            ImGui.TextUnformatted(text);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(w, h));
    }
}
