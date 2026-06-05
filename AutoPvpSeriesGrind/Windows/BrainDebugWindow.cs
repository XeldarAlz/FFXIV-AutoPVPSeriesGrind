using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed class BrainDebugWindow : Window, IDisposable
{
    private const float HpHealthy = 0.5f;
    private const float HpWounded = 0.25f;
    private const float NearYou = 20f; // display radius for the "near you" force readout

    public BrainDebugWindow() : base("Auto PVP Series Grind: Brain###AutoPvpSeriesGrindBrain")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(400, 520);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(340, 420),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();
        var cfg = Plugin.Cfg;

        DrawTopStrip(cfg);
        HairlineRule();

        if (!cfg.EnableCombatBrain)
        {
            Centered("The combat brain is off.", Styling.TextSecondary);
            Centered("Turn it on in Settings → Combat.", Styling.TextMuted);
            return;
        }

        var inMatch = Plugin.Instance.Controller.Phase == AutoPhase.InMatch;
        if (BrainTelemetry.Plan is not { } plan || (!inMatch && !BrainTelemetry.IsFresh))
        {
            Styling.VSpace(8);
            Centered(inMatch ? "Match starting — the brain spins up at the gate." : "Not in a live match.", Styling.TextMuted);
            return;
        }

        var snap = inMatch ? MatchState.Capture() : BrainTelemetry.Snapshot;
        if (snap is null) return;

        Styling.VSpace(6);
        DrawDecision(plan);
        Styling.VSpace(8);
        DrawRadar(snap, plan);
        Styling.VSpace(2);
        DrawLegend();
        Styling.VSpace(12);
        DrawHpBar(snap.SelfHp);
        Styling.VSpace(10);
        DrawMetrics(snap, plan);
    }

    private static void DrawTopStrip(Configuration cfg)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"{Plugin.Instance.Controller.Phase}   ·   {cfg.Strategy}   ·   humanize {cfg.Humanize}");

        if (!NavIpc.Instance.IsAvailable)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.AccentRose))
                ImGui.TextUnformatted("vnavmesh IPC unavailable — chat fallback in use.");
    }

    private static void DrawDecision(in MovePlan plan)
    {
        var s = ImGuiHelpers.GlobalScale;
        var col = PostureColor(plan.Posture);
        var icon = PostureIcon(plan.Posture);
        var label = plan.Posture.ToString().ToUpperInvariant();

        ImGui.SetWindowFontScale(1.3f);
        var iconStr = icon.ToIconString();
        Vector2 iconSize;
        using (ImRaii.PushFont(UiBuilder.IconFont))
            iconSize = ImGui.CalcTextSize(iconStr);
        var labelSize = ImGui.CalcTextSize(label);
        var gap = 9f * s;
        var total = iconSize.X + gap + labelSize.X;
        Styling.CenterNextItem(total);

        using (ImRaii.PushFont(UiBuilder.IconFont))
        using (ImRaii.PushColor(ImGuiCol.Text, col))
            ImGui.TextUnformatted(iconStr);
        ImGui.SameLine(0, gap);
        using (ImRaii.PushColor(ImGuiCol.Text, col))
            ImGui.TextUnformatted(label);
        ImGui.SetWindowFontScale(1f);

        if (!string.IsNullOrWhiteSpace(plan.Reason))
        {
            Styling.VSpace(2);
            Centered(plan.Reason, Styling.TextSecondary);
        }
    }

    private static void DrawRadar(PvpSnapshot snap, in MovePlan plan)
    {
        var s = ImGuiHelpers.GlobalScale;
        var size = MathF.Min(ImGui.GetContentRegionAvail().X, 210f * s);
        var radius = size * 0.5f;
        var startScreen = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availX * 0.5f, startScreen.Y + radius);
        var dl = ImGui.GetWindowDrawList();

        dl.AddCircleFilled(center, radius, ImGui.GetColorU32(Styling.WithAlpha(Styling.CardBg, 0.9f)));
        ProgressRing.Track(center, radius, 1.2f * s, Styling.WithAlpha(Styling.BorderDim, 0.85f));
        ProgressRing.Track(center, radius * 0.66f, 1f * s, Styling.WithAlpha(Styling.BorderDim, 0.5f));
        ProgressRing.Track(center, radius * 0.33f, 1f * s, Styling.WithAlpha(Styling.BorderDim, 0.35f));
        var cross = ImGui.GetColorU32(Styling.WithAlpha(Styling.BorderDim, 0.30f));
        dl.AddLine(center with { X = center.X - radius }, center with { X = center.X + radius }, cross, 1f);
        dl.AddLine(center with { Y = center.Y - radius }, center with { Y = center.Y + radius }, cross, 1f);

        var maxR = 18f;
        foreach (var e in snap.Enemies) maxR = MathF.Max(maxR, e.DistanceToSelf);
        foreach (var a in snap.Allies) maxR = MathF.Max(maxR, a.DistanceToSelf);
        if (snap.Objective is { } objc) maxR = MathF.Max(maxR, Horiz(snap.Self, objc));
        maxR = MathF.Max(maxR, Horiz(snap.Self, plan.Destination));
        maxR = Math.Clamp(maxR * 1.12f, 18f, 45f);

        var edge = radius - 7f * s;

        if (plan.Kind != MoveKind.Hold)
        {
            var d = Project(plan.Destination, snap.Self, radius, maxR, center, edge);
            dl.AddLine(center, d, ImGui.GetColorU32(Styling.WithAlpha(PostureColor(plan.Posture), 0.5f)), 1.5f * s);
            ProgressRing.Track(d, 5f * s, 1.5f * s, PostureColor(plan.Posture));
        }

        if (snap.Objective is { } obj)
        {
            var p = Project(obj, snap.Self, radius, maxR, center, edge);
            var hs = 4f * s;
            dl.AddRectFilled(p - new Vector2(hs, hs), p + new Vector2(hs, hs), ImGui.GetColorU32(Styling.AccentAmber), 1.5f);
        }

        foreach (var a in snap.Allies)
            dl.AddCircleFilled(Project(a.Position, snap.Self, radius, maxR, center, edge), 3.2f * s,
                ImGui.GetColorU32(Styling.WithAlpha(Styling.AccentMint, 0.9f)));

        foreach (var e in snap.Enemies)
        {
            var p = Project(e.Position, snap.Self, radius, maxR, center, edge);
            dl.AddCircleFilled(p, 3.4f * s, ImGui.GetColorU32(Styling.AccentRose));
            if (e.IsCasting)
                ProgressRing.Track(p, 5.5f * s, 1.2f * s, Styling.WithAlpha(Styling.AccentAmberSoft, 0.9f));
        }

        dl.AddCircleFilled(center, 4.5f * s, ImGui.GetColorU32(Styling.AccentVioletSoft));
        ProgressRing.Track(center, 4.5f * s, 1.2f * s, Styling.WithAlpha(Styling.TextStrong, 0.6f));

        ImGui.SetCursorScreenPos(startScreen);
        ImGui.Dummy(new Vector2(availX, size));
        Styling.TextCentered($"~{(int)maxR}y range", Styling.TextMuted, 0.8f);
    }

    private static void DrawLegend()
    {
        var s = ImGuiHelpers.GlobalScale;
        (string Txt, Vector4 Col)[] parts =
        [
            ("● enemy", Styling.AccentRose),
            ("● ally", Styling.AccentMint),
            ("■ objective", Styling.AccentAmber),
            ("● you", Styling.AccentVioletSoft),
        ];

        ImGui.SetWindowFontScale(0.82f);
        var gap = 12f * s;
        var total = 0f;
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0) total += gap;
            total += ImGui.CalcTextSize(parts[i].Txt).X;
        }
        Styling.CenterNextItem(total);
        for (var i = 0; i < parts.Length; i++)
        {
            if (i > 0) ImGui.SameLine(0, gap);
            using (ImRaii.PushColor(ImGuiCol.Text, parts[i].Col))
                ImGui.TextUnformatted(parts[i].Txt);
        }
        ImGui.SetWindowFontScale(1f);
    }

    private static void DrawHpBar(float hp)
    {
        var s = ImGuiHelpers.GlobalScale;
        var col = hp > HpHealthy ? Styling.AccentMint : hp > HpWounded ? Styling.AccentAmber : Styling.AccentRose;
        var w = ImGui.GetContentRegionAvail().X;
        var h = ImGui.GetFrameHeight() * 0.85f;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(w, h);
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), h * 0.5f);
        if (hp > 0)
            dl.AddRectFilled(origin, new Vector2(origin.X + w * Math.Clamp(hp, 0f, 1f), end.Y), ImGui.GetColorU32(col), h * 0.5f);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.BorderDim), h * 0.5f);

        var text = $"HP {hp:P0}";
        var ts = ImGui.CalcTextSize(text);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + (w - ts.X) * 0.5f, origin.Y + (h - ts.Y) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(text);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(w, h));
    }

    private static void DrawMetrics(PvpSnapshot snap, in MovePlan plan)
    {
        using var table = ImRaii.Table("##brain_metrics", 2,
            ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.PadOuterX);
        if (!table) return;

        ImGui.TableSetupColumn("##k", ImGuiTableColumnFlags.WidthFixed, 140f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("##v", ImGuiTableColumnFlags.WidthStretch);

        var alliesNear = snap.AlliesWithin(NearYou);
        var enemiesNear = snap.EnemiesWithin(NearYou);
        var nearColor = 1 + alliesNear >= enemiesNear ? Styling.AccentMint : Styling.AccentRose;

        Row("Forces", $"{snap.Enemies.Count} enemy  ·  {snap.Allies.Count} ally", Styling.TextStrong);
        Row($"Near you (≤{(int)NearYou}y)", $"{1 + alliesNear} ally  ·  {enemiesNear} enemy", nearColor);
        Row("Focused by", snap.FocusCount.ToString(), snap.FocusCount > 0 ? Styling.AccentRose : Styling.TextStrong);
        Row("Nearest ally", snap.Allies.Count == 0 ? "—" : $"{snap.NearestAllyDistance:F1}y",
            snap.Allies.Count == 0 ? Styling.AccentRose : Styling.TextStrong);
        Row("Nearest enemy", snap.Enemies.Count == 0 ? "—" : $"{snap.NearestEnemyDistance:F1}y", Styling.TextStrong);
        Row("Position", snap.PrefersBackline ? "backline" : "frontline", Styling.TextStrong);
        Row("Objective", snap.HasObjective ? $"located  ·  {Horiz(snap.Self, snap.Objective!.Value):F0}y" : "not found",
            snap.HasObjective ? Styling.AccentMint : Styling.TextMuted);
        Row("Move target", plan.Kind == MoveKind.Hold ? "holding" : $"{Horiz(snap.Self, plan.Destination):F1}y away{(plan.Sprint ? "  ·  sprint" : "")}",
            Styling.TextStrong);
    }

    private static void Row(string label, string value, Vector4 valueColor)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(label);
        ImGui.TableSetColumnIndex(1);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, valueColor))
            ImGui.TextUnformatted(value);
    }

    private static Vector4 PostureColor(Posture posture) => posture switch
    {
        Posture.Retreat => Styling.AccentRose,
        Posture.Regroup => Styling.AccentAmberSoft,
        Posture.Reposition => Styling.AccentAmber,
        Posture.Stage => Styling.AccentVioletSoft,
        Posture.Push => Styling.AccentAmber,
        Posture.Hold => Styling.AccentMint,
        _ => Styling.TextMuted,
    };

    private static FontAwesomeIcon PostureIcon(Posture posture) => posture switch
    {
        Posture.Retreat => FontAwesomeIcon.Running,
        Posture.Regroup => FontAwesomeIcon.Users,
        Posture.Reposition => FontAwesomeIcon.Walking,
        Posture.Stage => FontAwesomeIcon.HourglassHalf,
        Posture.Push => FontAwesomeIcon.Crosshairs,
        _ => FontAwesomeIcon.ShieldAlt,
    };

    private static float Horiz(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static Vector2 Project(Vector3 pos, Vector3 self, float radius, float maxR, Vector2 center, float edge)
    {
        var v = new Vector2(pos.X - self.X, pos.Z - self.Z) * (radius / maxR);
        var len = v.Length();
        if (len > edge && len > 0f) v *= edge / len;
        return center + v;
    }

    private static void Centered(string text, Vector4 color) => Styling.TextCentered(text, color);

    private static void HairlineRule()
    {
        Styling.VSpace(3f);
        var dl = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();
        var w = ImGui.GetContentRegionAvail().X;
        dl.AddLine(p, p + new Vector2(w, 0), ImGui.GetColorU32(Styling.Hairline), 1f);
        ImGui.Dummy(new Vector2(w, 1f));
        Styling.VSpace(3f);
    }
}
