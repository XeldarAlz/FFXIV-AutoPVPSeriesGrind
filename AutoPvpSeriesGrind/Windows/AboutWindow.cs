using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed partial class AboutWindow : Window, IDisposable
{
    private const string Name = "Auto PVP Series Grind";
    private const string RepoUrl = "https://github.com/XeldarAlz/FFXIV-AutoPVPSeriesGrind";
    private const string IconFile = "Icon.png";
    private const string WindowId = "AutoPvpSeriesGrindAbout";

    private const string PatreonUrl = "https://www.patreon.com/XeldarAlz";
    private const string DiscordUrl = "https://discord.gg/3HbJCscMyS";
    private const string HubUrl = "https://github.com/XeldarAlz/DalamudPlugins";
    private const string Author = "XeldarAlz";

    private const string IssuesUrl = RepoUrl + "/issues";
    private const string DiscussionsUrl = RepoUrl + "/discussions";
    private const string SecurityUrl = RepoUrl + "/security/advisories/new";

    private static readonly (FontAwesomeIcon Icon, string Label, string Url, int AccentId)[] Links =
    {
        (FontAwesomeIcon.CodeBranch, "GitHub", RepoUrl, 0),
        (FontAwesomeIcon.Hashtag, "Discord", DiscordUrl, 5),
        (FontAwesomeIcon.Comments, "Discussions", DiscussionsUrl, 1),
        (FontAwesomeIcon.Bug, "Report a bug", IssuesUrl, 2),
        (FontAwesomeIcon.ThLarge, "More plugins", HubUrl, 3),
        (FontAwesomeIcon.ShieldAlt, "Security", SecurityUrl, 4),
    };

    private static readonly Vector2[] BloomOffsets =
    {
        new(1.6f, 0f), new(-1.6f, 0f), new(0f, 1.6f), new(0f, -1.6f),
    };

    private long openTick = long.MinValue / 2;

    public AboutWindow() : base($"{Name}: About###{WindowId}")
    {
        Size = new Vector2(540, 700);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(430, 580),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void OnOpen() => openTick = Environment.TickCount64;

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, MathF.Max(0.0001f, Reveal(0))))
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
            SectionHeader(FontAwesomeIcon.Link, "Connect", Styling.AccentBlue);
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
        return Smooth01(Math.Clamp((float)progress, 0f, 1f));
    }

    private void RevealSection(int index, Action draw)
    {
        var a = Reveal(index);
        if (a < 1f)
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (1f - a) * 12f * ImGuiHelpers.GlobalScale);
        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, MathF.Max(0.0001f, a)))
            draw();
    }

    private static void AmbientBackground()
    {
        var wpos = ImGui.GetWindowPos();
        var rmin = wpos + ImGui.GetWindowContentRegionMin();
        var rmax = wpos + ImGui.GetWindowContentRegionMax();
        var w = rmax.X - rmin.X;
        var h = rmax.Y - rmin.Y;

        var dl = ImGui.GetWindowDrawList();
        dl.PushClipRect(rmin, rmax, true);

        SoftBlob(rmin + new Vector2(w * (0.26f + 0.12f * Wave(11000)), h * (0.20f + 0.10f * Wave(13700))),
            w * 0.55f, Styling.AccentViolet, 0.075f);
        SoftBlob(rmin + new Vector2(w * (0.80f + 0.12f * Wave(15500)), h * (0.32f + 0.10f * Wave(9300))),
            w * 0.48f, Styling.AccentPink, 0.060f);
        SoftBlob(rmin + new Vector2(w * (0.55f + 0.14f * Wave(17900)), h * (0.82f + 0.08f * Wave(12100))),
            w * 0.52f, Styling.AccentBlue, 0.050f);

        dl.PopClipRect();
    }

    private static void SoftBlob(Vector2 c, float radius, Vector4 color, float peak)
    {
        var dl = ImGui.GetWindowDrawList();
        const int layers = 5;
        for (var i = layers; i >= 1; i--)
        {
            var r = radius * i / layers;
            var a = peak * (1f - (i - 1f) / layers);
            dl.AddCircleFilled(c, r, ImGui.GetColorU32(Styling.WithAlpha(color, a)), 40);
        }
    }

    private static float Wave(double periodMs)
        => MathF.Sin((float)(Environment.TickCount % periodMs / periodMs) * MathF.PI * 2f);

    private static float Smooth01(float x) => x * x * (3f - 2f * x);

    private static float Heartbeat(double periodMs)
    {
        var p = Styling.Phase(periodMs);
        return MathF.Max(Bump(p, 0.06f, 0.06f), Bump(p, 0.20f, 0.06f) * 0.6f);
    }

    private static float Bump(float p, float center, float width)
    {
        var d = (p - center) / width;
        if (d < -1f || d > 1f) return 0f;
        return 0.5f * (1f + MathF.Cos(d * MathF.PI));
    }

    private static Vector4 Lighten(Vector4 c, float t)
        => Vector4.Lerp(c, Styling.White, t) with { W = c.W };
}
