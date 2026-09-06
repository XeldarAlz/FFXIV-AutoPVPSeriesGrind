using AutoPvpSeriesGrind.Core.Localization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

// Per-plugin constants live at the top of this file; the rest of the page is a template shared
// across the plugin family and should stay identical everywhere.
internal sealed partial class AboutPage
{
    private const string Name = "Auto PVP Series Grind";
    private const string RepoUrl = "https://github.com/XeldarAlz/FFXIV-AutoPVPSeriesGrind";

    private const string PatreonUrl = "https://www.patreon.com/XeldarAlz";
    private const string DiscordUrl = "https://discord.gg/3HbJCscMyS";
    private const string HubUrl = "https://github.com/XeldarAlz/DalamudPlugins";
    private const string Author = "XeldarAlz";

    private const string IssuesUrl = RepoUrl + "/issues";
    private const string DiscussionsUrl = RepoUrl + "/discussions";
    private const string SecurityUrl = RepoUrl + "/security/advisories/new";

    private static readonly (FontAwesomeIcon Icon, LocString Label, string Url, int AccentId)[] Links =
    {
        (FontAwesomeIcon.CodeBranch, L.About.LinkGitHub, RepoUrl, 0),
        (FontAwesomeIcon.Hashtag, L.About.LinkDiscord, DiscordUrl, 5),
        (FontAwesomeIcon.Comments, L.About.LinkDiscussions, DiscussionsUrl, 1),
        (FontAwesomeIcon.Bug, L.About.LinkBug, IssuesUrl, 2),
        (FontAwesomeIcon.ThLarge, L.About.LinkMore, HubUrl, 3),
        (FontAwesomeIcon.ShieldAlt, L.About.LinkSecurity, SecurityUrl, 4),
    };

    private static readonly Vector2[] BloomOffsets =
    {
        new(1.6f, 0f), new(-1.6f, 0f), new(0f, 1.6f), new(0f, -1.6f),
    };

    private long openTick = long.MinValue / 2;

    public void Draw(long shownTick)
    {
        openTick = shownTick;

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, MathF.Max(0.0001f, Reveal(0) * ImGui.GetStyle().Alpha)))
            AmbientBackground();

        RevealSection(0, () =>
        {
            DrawHero();
            Styling.VSpace(16);
        });
        RevealSection(1, () =>
        {
            DrawSupport();
            Styling.VSpace(16);
        });
        RevealSection(2, () =>
        {
            SectionHeader(FontAwesomeIcon.Link, Loc.T(L.About.Connect), Styling.AccentBlue);
            Styling.VSpace(6);
            DrawConnect();
            Styling.VSpace(16);
        });
        RevealSection(3, DrawFooter);
    }

    private float Reveal(int index)
    {
        const float durationMs = 420f;
        const float staggerMs = 95f;
        var elapsed = Environment.TickCount64 - openTick;
        var progress = (elapsed - index * staggerMs) / durationMs;
        return Motion.Smoothstep(Math.Clamp((float)progress, 0f, 1f));
    }

    private void RevealSection(int index, Action draw)
    {
        var alpha = Reveal(index);
        if (alpha < 1f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (1f - alpha) * 12f * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, MathF.Max(0.0001f, alpha * ImGui.GetStyle().Alpha)))
            draw();
    }

    private static void AmbientBackground()
    {
        var windowPos = ImGui.GetWindowPos();
        var min = windowPos + ImGui.GetWindowContentRegionMin();
        var max = windowPos + ImGui.GetWindowContentRegionMax();
        var width = max.X - min.X;
        var height = max.Y - min.Y;

        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(min, max, true);

        SoftBlob(min + new Vector2(width * (0.26f + 0.12f * Motion.Wave(11000)), height * (0.20f + 0.10f * Motion.Wave(13700))),
            width * 0.55f, Styling.AccentArc, 0.075f);
        SoftBlob(min + new Vector2(width * (0.80f + 0.12f * Motion.Wave(15500)), height * (0.32f + 0.10f * Motion.Wave(9300))),
            width * 0.48f, Styling.AccentMagenta, 0.060f);
        SoftBlob(min + new Vector2(width * (0.55f + 0.14f * Motion.Wave(17900)), height * (0.82f + 0.08f * Motion.Wave(12100))),
            width * 0.52f, Styling.AccentBlue, 0.050f);

        dl.PopClipRect();
    }

    private static void SoftBlob(Vector2 center, float radius, Vector4 color, float peak)
    {
        var dl = ImGui.GetWindowDrawList();
        const int layers = 5;
        for (var layer = layers; layer >= 1; layer--)
        {
            var layerRadius = radius * layer / layers;
            var alpha = peak * (1f - (layer - 1f) / layers);
            dl.AddCircleFilled(center, layerRadius, Paint.Col(Styling.WithAlpha(color, alpha)), 40);
        }
    }

    private static float Heartbeat(double periodMs)
    {
        var phase = Styling.Phase(periodMs);
        return MathF.Max(Bump(phase, 0.06f, 0.06f), Bump(phase, 0.20f, 0.06f) * 0.6f);
    }

    private static float Bump(float phase, float center, float width)
    {
        var distance = (phase - center) / width;
        if (distance < -1f || distance > 1f) return 0f;
        return 0.5f * (1f + MathF.Cos(distance * MathF.PI));
    }
}
