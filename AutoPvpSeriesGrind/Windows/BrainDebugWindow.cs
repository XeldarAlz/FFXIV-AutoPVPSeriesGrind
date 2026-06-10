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
    private const float NearYouRadiusYalms = 20f;
    private const string NearYouLabel = "Near you (≤20y)";

    private static readonly (string Text, Vector4 Color)[] LegendParts =
    {
        ("● enemy", Styling.AccentRose),
        ("● ally", Styling.AccentMint),
        ("■ objective", Styling.AccentAmber),
        ("● you", Styling.AccentVioletSoft),
    };

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
        Styling.HairlineRule(3f, 3f);

        if (!cfg.EnableCombatBrain)
        {
            Styling.TextCentered("The combat brain is off.", Styling.TextSecondary);
            Styling.TextCentered("Turn it on in Settings → Combat.", Styling.TextMuted);
            return;
        }

        var inMatch = Plugin.Instance.Controller.Phase == AutoPhase.InMatch;
        if (BrainTelemetry.Plan is not { } plan || (!inMatch && !BrainTelemetry.IsFresh))
        {
            Styling.VSpace(8);
            Styling.TextCentered(inMatch ? "Match starting — the brain spins up at the gate." : "Not in a live match.", Styling.TextMuted);
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
            Styling.TextCentered(plan.Reason, Styling.TextSecondary);
        }
    }

    private static void DrawRadar(PvpSnapshot snap, in MovePlan plan)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = MathF.Min(ImGui.GetContentRegionAvail().X, 210f * scale);
        var radius = size * 0.5f;
        var startScreen = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availX * 0.5f, startScreen.Y + radius);

        DrawRadarBackground(center, radius, scale);
        var maxRange = ComputeRadarRange(snap, plan);
        DrawRadarMarkers(snap, plan, center, radius, maxRange, scale);

        ImGui.SetCursorScreenPos(startScreen);
        ImGui.Dummy(new Vector2(availX, size));
        Styling.TextCentered($"~{(int)maxRange}y range", Styling.TextMuted, 0.8f);
    }

    private static void DrawRadarBackground(Vector2 center, float radius, float scale)
    {
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddCircleFilled(center, radius, ImGui.GetColorU32(Styling.WithAlpha(Styling.CardBg, 0.9f)));
        ProgressRing.Track(center, radius, 1.2f * scale, Styling.WithAlpha(Styling.BorderDim, 0.85f));
        ProgressRing.Track(center, radius * 0.66f, 1f * scale, Styling.WithAlpha(Styling.BorderDim, 0.5f));
        ProgressRing.Track(center, radius * 0.33f, 1f * scale, Styling.WithAlpha(Styling.BorderDim, 0.35f));
        var cross = ImGui.GetColorU32(Styling.WithAlpha(Styling.BorderDim, 0.30f));
        drawList.AddLine(center with { X = center.X - radius }, center with { X = center.X + radius }, cross, 1f);
        drawList.AddLine(center with { Y = center.Y - radius }, center with { Y = center.Y + radius }, cross, 1f);
    }

    private static float ComputeRadarRange(PvpSnapshot snap, in MovePlan plan)
    {
        var maxRange = 18f;
        for (var enemyIndex = 0; enemyIndex < snap.Enemies.Count; enemyIndex++)
        {
            maxRange = MathF.Max(maxRange, snap.Enemies[enemyIndex].DistanceToSelf);
        }
        for (var allyIndex = 0; allyIndex < snap.Allies.Count; allyIndex++)
        {
            maxRange = MathF.Max(maxRange, snap.Allies[allyIndex].DistanceToSelf);
        }
        if (snap.Objective is { } objectivePosition)
        {
            maxRange = MathF.Max(maxRange, HorizontalDistance(snap.Self, objectivePosition));
        }
        maxRange = MathF.Max(maxRange, HorizontalDistance(snap.Self, plan.Destination));
        return Math.Clamp(maxRange * 1.12f, 18f, 45f);
    }

    private static void DrawRadarMarkers(PvpSnapshot snap, in MovePlan plan, Vector2 center, float radius, float maxRange, float scale)
    {
        var drawList = ImGui.GetWindowDrawList();
        var edge = radius - 7f * scale;

        if (plan.Kind != MoveKind.Hold)
        {
            var destination = Project(plan.Destination, snap.Self, radius, maxRange, center, edge);
            drawList.AddLine(center, destination, ImGui.GetColorU32(Styling.WithAlpha(PostureColor(plan.Posture), 0.5f)), 1.5f * scale);
            ProgressRing.Track(destination, 5f * scale, 1.5f * scale, PostureColor(plan.Posture));
        }

        if (snap.Objective is { } objective)
        {
            var projected = Project(objective, snap.Self, radius, maxRange, center, edge);
            var halfSquare = 4f * scale;
            drawList.AddRectFilled(projected - new Vector2(halfSquare, halfSquare), projected + new Vector2(halfSquare, halfSquare), ImGui.GetColorU32(Styling.AccentAmber), 1.5f);
        }

        for (var allyIndex = 0; allyIndex < snap.Allies.Count; allyIndex++)
        {
            drawList.AddCircleFilled(Project(snap.Allies[allyIndex].Position, snap.Self, radius, maxRange, center, edge), 3.2f * scale,
                ImGui.GetColorU32(Styling.WithAlpha(Styling.AccentMint, 0.9f)));
        }

        for (var enemyIndex = 0; enemyIndex < snap.Enemies.Count; enemyIndex++)
        {
            var enemy = snap.Enemies[enemyIndex];
            var projected = Project(enemy.Position, snap.Self, radius, maxRange, center, edge);
            drawList.AddCircleFilled(projected, 3.4f * scale, ImGui.GetColorU32(Styling.AccentRose));
            if (enemy.IsCasting)
            {
                ProgressRing.Track(projected, 5.5f * scale, 1.2f * scale, Styling.WithAlpha(Styling.AccentAmberSoft, 0.9f));
            }
        }

        drawList.AddCircleFilled(center, 4.5f * scale, ImGui.GetColorU32(Styling.AccentVioletSoft));
        ProgressRing.Track(center, 4.5f * scale, 1.2f * scale, Styling.WithAlpha(Styling.TextStrong, 0.6f));
    }

    private static void DrawLegend()
    {
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetWindowFontScale(0.82f);
        var gap = 12f * scale;
        var total = 0f;
        for (var partIndex = 0; partIndex < LegendParts.Length; partIndex++)
        {
            if (partIndex > 0)
            {
                total += gap;
            }
            total += ImGui.CalcTextSize(LegendParts[partIndex].Text).X;
        }
        Styling.CenterNextItem(total);
        for (var partIndex = 0; partIndex < LegendParts.Length; partIndex++)
        {
            if (partIndex > 0)
            {
                ImGui.SameLine(0, gap);
            }
            using (ImRaii.PushColor(ImGuiCol.Text, LegendParts[partIndex].Color))
                ImGui.TextUnformatted(LegendParts[partIndex].Text);
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

        var alliesNear = snap.AlliesWithin(NearYouRadiusYalms);
        var enemiesNear = snap.EnemiesWithin(NearYouRadiusYalms);
        var nearColor = 1 + alliesNear >= enemiesNear ? Styling.AccentMint : Styling.AccentRose;

        Row("Forces", $"{snap.Enemies.Count} enemy  ·  {snap.Allies.Count} ally", Styling.TextStrong);
        Row(NearYouLabel, $"{1 + alliesNear} ally  ·  {enemiesNear} enemy", nearColor);
        Row("Focused by", snap.FocusCount.ToString(), snap.FocusCount > 0 ? Styling.AccentRose : Styling.TextStrong);
        Row("Nearest ally", snap.Allies.Count == 0 ? "—" : $"{snap.NearestAllyDistance:F1}y",
            snap.Allies.Count == 0 ? Styling.AccentRose : Styling.TextStrong);
        Row("Nearest enemy", snap.Enemies.Count == 0 ? "—" : $"{snap.NearestEnemyDistance:F1}y", Styling.TextStrong);
        Row("Position", snap.PrefersBackline ? "backline" : "frontline", Styling.TextStrong);
        Row("Objective", snap.HasObjective ? $"located  ·  {HorizontalDistance(snap.Self, snap.Objective!.Value):F0}y" : "not found",
            snap.HasObjective ? Styling.AccentMint : Styling.TextMuted);
        Row("Move target", plan.Kind == MoveKind.Hold ? "holding" : $"{HorizontalDistance(snap.Self, plan.Destination):F1}y away{(plan.Sprint ? "  ·  sprint" : "")}",
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

    private static float HorizontalDistance(Vector3 a, Vector3 b)
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
}
