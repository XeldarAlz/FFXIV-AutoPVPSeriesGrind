using AutoPvpSeriesGrind.Core.Combat;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Components;

internal static class BrainMinimap
{
    private const float RangeMin = 18f;
    private const float RangeMax = 45f;
    private const float RangePadding = 1.12f;
    private const float EngageRingYalms = 20f;
    private const float ContestRadiusYalms = 5f;

    private const int GridSegments = 72;
    private const float GridEdgeInset = 7f;
    private const float GridRingThickness = 1.2f;
    private const float GridSubRingThickness = 1f;
    private const float GridMidRingScale = 0.66f;
    private const float GridInnerRingScale = 0.33f;
    private const float GridBackgroundAlpha = 0.92f;
    private const float GridOuterAlpha = 0.85f;
    private const float GridMidAlpha = 0.5f;
    private const float GridInnerAlpha = 0.35f;
    private const float GridCrossAlpha = 0.30f;
    private const float EngageRingAlpha = 0.16f;

    private const float SweepInset = 1.5f;
    private const float SweepThickness = 1.4f;
    private const float SweepAlpha = 0.45f;
    private const float SweepArc = MathF.PI * 0.55f;
    private const float SweepHeadAlpha = 0.5f;

    private const float FacingLength = 24f;
    private const float FacingHalfAngle = 0.45f;
    private const int FacingRays = 11;
    private const float FacingBaseAlpha = 0.10f;
    private const float FacingFalloff = 0.65f;
    private const float FacingThickness = 2f;
    private const float FacingTipExtra = 2f;
    private const float FacingTipAlpha = 0.55f;
    private const float FacingTipThickness = 1.6f;

    private const int DiamondSegments = 4;
    private const float ObjectiveSize = 5f;
    private const float ObjectiveCoreScale = 0.45f;
    private const float ObjectiveCoreAlpha = 0.85f;
    private const float ObjectiveGlowBase = 0.25f;
    private const float ObjectiveGlowPulse = 0.35f;

    private const float WaypointRadius = 5f;
    private const float WaypointRadiusEnergy = 3f;
    private const float WaypointRingThickness = 1.6f;
    private const float WaypointLineAlpha = 0.45f;
    private const float WaypointLineAlphaEnergy = 0.45f;
    private const float WaypointLineThickness = 1.5f;
    private const float WaypointLineThicknessEnergy = 1f;
    private const float WaypointGlowBase = 0.35f;
    private const float WaypointGlowPulse = 0.3f;
    private const float WaypointGlowEnergy = 1.3f;
    private const float WaypointShockOffset = 4f;
    private const float WaypointShockSpread = 22f;
    private const float WaypointShockThickness = 0.5f;
    private const float WaypointShockThicknessEnergy = 2f;

    private const float ReticleTickInner = 1f;
    private const float ReticleTickOuter = 4f;
    private const float ReticleTickThickness = 1.5f;

    private const float PillPadX = 6f;
    private const float PillPadY = 3f;
    private const float PillBackgroundAlpha = 0.88f;
    private const float PillBorderAlpha = 0.55f;
    private const float BadgeInset = 4f;

    private const float RangeLabelInset = 3f;

    private static readonly Dot PlayerNode = new(4.8f, Styling.AccentVioletSoft);
    private static readonly Ring PlayerNodeRing = new(4.8f, 1.2f, Styling.WithAlpha(Styling.TextStrong, 0.7f));
    private static readonly Dot EnemyDot = new(3.6f, Styling.AccentRose);
    private static readonly Dot AllyDot = new(3.2f, Styling.WithAlpha(Styling.AccentMint, 0.9f));
    private static readonly Dot FocusHalo = new(7.5f, Styling.WithAlpha(Styling.AccentRose, 0.16f));
    private static readonly Ring FocusRing = new(6.5f, 1f, Styling.WithAlpha(Styling.AccentRose, 0.7f));
    private static readonly Ring GuardRing = new(4.8f, 1f, Styling.WithAlpha(Styling.AccentBlue, 0.85f));
    private static readonly Ring CastRing = new(9f, 1.2f, Styling.WithAlpha(Styling.AccentAmberSoft, 0.9f));
    private static readonly Ring ReticleRing = new(7.5f, 1.5f, Styling.AccentVioletSoft);

    private readonly record struct Dot(float Radius, Vector4 Color);

    private readonly record struct Ring(float Radius, float Thickness, Vector4 Color);

    public static void Draw(PvpSnapshot snap, in MovePlan plan, float diameter, float destinationEnergy = 0f)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var availX = ImGui.GetContentRegionAvail().X;
        var origin = ImGui.GetCursorScreenPos();
        var radius = diameter * 0.5f;
        var center = new Vector2(origin.X + availX * 0.5f, origin.Y + radius);
        var edge = radius - GridEdgeInset * scale;
        var range = ComputeRange(snap, plan);

        DrawGrid(center, radius, edge, range, scale);
        DrawSweep(center, radius, plan, scale);
        DrawFacing(snap, center, scale);
        DrawDestination(snap, plan, center, radius, range, edge, scale, destinationEnergy);
        DrawObjective(snap, center, radius, range, edge, scale);
        DrawAllies(snap, center, radius, range, edge, scale);
        DrawEnemies(snap, plan, center, radius, range, edge, scale);
        DrawPlayer(center, scale);
        DrawBadges(snap, center, radius, scale);
        DrawRange(center, radius, range, scale);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(availX, diameter));
    }

    private static void DrawGrid(Vector2 center, float radius, float edge, float range, float scale)
    {
        var dl = ImGui.GetWindowDrawList();
        dl.AddCircleFilled(center, radius, ImGui.GetColorU32(Styling.WithAlpha(Styling.CardBg, GridBackgroundAlpha)), GridSegments);
        ProgressRing.Track(center, radius, GridRingThickness * scale, Styling.WithAlpha(Styling.BorderDim, GridOuterAlpha));
        ProgressRing.Track(center, radius * GridMidRingScale, GridSubRingThickness * scale, Styling.WithAlpha(Styling.BorderDim, GridMidAlpha));
        ProgressRing.Track(center, radius * GridInnerRingScale, GridSubRingThickness * scale, Styling.WithAlpha(Styling.BorderDim, GridInnerAlpha));

        var engage = radius * (EngageRingYalms / range);
        if (engage < edge)
            ProgressRing.Track(center, engage, GridSubRingThickness * scale, Styling.WithAlpha(Styling.AccentVioletSoft, EngageRingAlpha));

        var cross = ImGui.GetColorU32(Styling.WithAlpha(Styling.BorderDim, GridCrossAlpha));
        dl.AddLine(center with { X = center.X - radius }, center with { X = center.X + radius }, cross, scale);
        dl.AddLine(center with { Y = center.Y - radius }, center with { Y = center.Y + radius }, cross, scale);
    }

    private static void DrawSweep(Vector2 center, float radius, in MovePlan plan, float scale)
        => ProgressRing.Sweep(center, radius - SweepInset * scale, SweepThickness * scale,
            Styling.WithAlpha(PostureStyle.Color(plan.Posture), SweepAlpha), Styling.PulseOrbit, SweepArc, SweepHeadAlpha);

    private static void DrawFacing(PvpSnapshot snap, Vector2 center, float scale)
    {
        var dl = ImGui.GetWindowDrawList();
        var heading = HeadingScreen(snap.SelfRotation);
        var length = FacingLength * scale;
        for (var rayIndex = 0; rayIndex < FacingRays; rayIndex++)
        {
            var t = rayIndex / (float)(FacingRays - 1);
            var angle = -FacingHalfAngle + t * FacingHalfAngle * 2f;
            var falloff = 1f - MathF.Abs(angle) / FacingHalfAngle * FacingFalloff;
            dl.AddLine(center, center + Rotate(heading, angle) * length,
                ImGui.GetColorU32(Styling.WithAlpha(Styling.AccentVioletSoft, FacingBaseAlpha * falloff)), FacingThickness * scale);
        }
        dl.AddLine(center, center + heading * (length + FacingTipExtra * scale),
            ImGui.GetColorU32(Styling.WithAlpha(Styling.AccentVioletSoft, FacingTipAlpha)), FacingTipThickness * scale);
    }

    private static void DrawDestination(PvpSnapshot snap, in MovePlan plan, Vector2 center, float radius, float range, float edge, float scale, float energy)
    {
        if (plan.Kind == MoveKind.Hold)
            return;
        var dl = ImGui.GetWindowDrawList();
        var color = PostureStyle.Color(plan.Posture);
        var dest = Project(plan.Destination, snap.Self, radius, range, center, edge);
        var marker = (WaypointRadius + WaypointRadiusEnergy * energy) * scale;

        dl.AddLine(center, dest, ImGui.GetColorU32(Styling.WithAlpha(color, WaypointLineAlpha + WaypointLineAlphaEnergy * energy)),
            (WaypointLineThickness + WaypointLineThicknessEnergy * energy) * scale);
        ProgressRing.Glow(dest, marker, color, WaypointGlowBase + WaypointGlowPulse * Styling.Pulse(Styling.PulseCalm) + WaypointGlowEnergy * energy);
        ProgressRing.Track(dest, marker, WaypointRingThickness * scale, color);

        if (energy > 0f)
            ProgressRing.Track(dest, marker + (WaypointShockOffset + WaypointShockSpread * (1f - energy)) * scale,
                (WaypointShockThickness + WaypointShockThicknessEnergy * energy) * scale, Styling.WithAlpha(color, energy));
    }

    private static void DrawObjective(PvpSnapshot snap, Vector2 center, float radius, float range, float edge, float scale)
    {
        if (snap.Objective is not { } objective)
            return;
        var dl = ImGui.GetWindowDrawList();
        var point = Project(objective, snap.Self, radius, range, center, edge);
        var contested = PvpSnapshot.CountWithin(snap.Enemies, objective, ContestRadiusYalms) > 0;
        var color = contested ? Styling.AccentAmber : Styling.AccentMint;
        var size = ObjectiveSize * scale;

        if (contested)
            ProgressRing.Glow(point, size, Styling.AccentAmber, ObjectiveGlowBase + ObjectiveGlowPulse * Styling.Pulse(Styling.PulseMedium));
        dl.AddCircleFilled(point, size, ImGui.GetColorU32(color), DiamondSegments);
        dl.AddCircleFilled(point, size * ObjectiveCoreScale, ImGui.GetColorU32(Styling.WithAlpha(Styling.White, ObjectiveCoreAlpha)), DiamondSegments);
    }

    private static void DrawAllies(PvpSnapshot snap, Vector2 center, float radius, float range, float edge, float scale)
    {
        for (var allyIndex = 0; allyIndex < snap.Allies.Count; allyIndex++)
            DrawDot(Project(snap.Allies[allyIndex].Position, snap.Self, radius, range, center, edge), AllyDot, scale);
    }

    private static void DrawEnemies(PvpSnapshot snap, in MovePlan plan, Vector2 center, float radius, float range, float edge, float scale)
    {
        for (var enemyIndex = 0; enemyIndex < snap.Enemies.Count; enemyIndex++)
        {
            var enemy = snap.Enemies[enemyIndex];
            var point = Project(enemy.Position, snap.Self, radius, range, center, edge);

            if (enemy.TargetId == snap.SelfId)
            {
                DrawDot(point, FocusHalo, scale);
                DrawRing(point, FocusRing, scale);
            }

            DrawDot(point, EnemyDot, scale);

            if (enemy.HasGuard)
                DrawRing(point, GuardRing, scale);
            if (enemy.IsCasting)
                DrawRing(point, CastRing, scale);
            if (plan.TargetId != 0 && enemy.Id == plan.TargetId)
                DrawReticle(point, scale);
        }
    }

    private static void DrawReticle(Vector2 point, float scale)
    {
        DrawRing(point, ReticleRing, scale);
        var dl = ImGui.GetWindowDrawList();
        var color = ImGui.GetColorU32(ReticleRing.Color);
        var inner = (ReticleRing.Radius + ReticleTickInner) * scale;
        var outer = (ReticleRing.Radius + ReticleTickOuter) * scale;
        var thickness = ReticleTickThickness * scale;
        dl.AddLine(point with { Y = point.Y - inner }, point with { Y = point.Y - outer }, color, thickness);
        dl.AddLine(point with { Y = point.Y + inner }, point with { Y = point.Y + outer }, color, thickness);
        dl.AddLine(point with { X = point.X - inner }, point with { X = point.X - outer }, color, thickness);
        dl.AddLine(point with { X = point.X + inner }, point with { X = point.X + outer }, color, thickness);
    }

    private static void DrawPlayer(Vector2 center, float scale)
    {
        DrawDot(center, PlayerNode, scale);
        DrawRing(center, PlayerNodeRing, scale);
    }

    private static void DrawBadges(PvpSnapshot snap, Vector2 center, float radius, float scale)
    {
        var inset = BadgeInset * scale;
        var alliesNear = snap.AlliesWithin(EngageRingYalms);
        var enemiesNear = snap.EnemiesWithin(EngageRingYalms);
        var advantage = 1 + alliesNear >= enemiesNear;
        Pill(new Vector2(center.X - radius + inset, center.Y - radius + inset), false,
            $"{1 + alliesNear}v{enemiesNear}", advantage ? Styling.AccentMint : Styling.AccentRose, scale);

        if (snap.FocusCount > 0)
            Pill(new Vector2(center.X + radius - inset, center.Y - radius + inset), true,
                $"focus x{snap.FocusCount}", Styling.PulseColor(Styling.AccentRose, Styling.AccentAmberSoft, Styling.PulseMedium), scale);
    }

    private static void Pill(Vector2 anchor, bool alignRight, string text, Vector4 color, float scale)
    {
        var dl = ImGui.GetWindowDrawList();
        using var font = Fonts.PushCaption();
        var textSize = TextDraw.Measure(text);
        var padX = PillPadX * scale;
        var padY = PillPadY * scale;
        var box = new Vector2(textSize.X + padX * 2f, textSize.Y + padY * 2f);
        var min = alignRight ? anchor with { X = anchor.X - box.X } : anchor;
        var max = min + box;

        Paint.Pill(dl, min, max, Styling.WithAlpha(Styling.Surface1, PillBackgroundAlpha), Styling.WithAlpha(color, PillBorderAlpha));
        TextDraw.At(text, new Vector2(min.X + padX, min.Y + padY), color);
    }

    private static void DrawRange(Vector2 center, float radius, float range, float scale)
    {
        var text = $"~{(int)range}y";
        using var font = Fonts.PushCaption();
        var textSize = TextDraw.Measure(text);
        TextDraw.At(text, new Vector2(center.X - textSize.X * 0.5f, center.Y + radius - textSize.Y - RangeLabelInset * scale), Styling.TextMuted);
    }

    private static void DrawDot(Vector2 position, Dot dot, float scale)
        => ImGui.GetWindowDrawList().AddCircleFilled(position, dot.Radius * scale, ImGui.GetColorU32(dot.Color));

    private static void DrawRing(Vector2 position, Ring ring, float scale)
        => ProgressRing.Track(position, ring.Radius * scale, ring.Thickness * scale, ring.Color);

    private static float ComputeRange(PvpSnapshot snap, in MovePlan plan)
    {
        var range = RangeMin;
        for (var enemyIndex = 0; enemyIndex < snap.Enemies.Count; enemyIndex++)
            range = MathF.Max(range, snap.Enemies[enemyIndex].DistanceToSelf);
        for (var allyIndex = 0; allyIndex < snap.Allies.Count; allyIndex++)
            range = MathF.Max(range, snap.Allies[allyIndex].DistanceToSelf);
        if (snap.Objective is { } objective)
            range = MathF.Max(range, HorizontalDistance(snap.Self, objective));
        range = MathF.Max(range, HorizontalDistance(snap.Self, plan.Destination));
        return Math.Clamp(range * RangePadding, RangeMin, RangeMax);
    }

    private static Vector2 Project(Vector3 position, Vector3 self, float radius, float range, Vector2 center, float edge)
    {
        var offset = new Vector2(position.X - self.X, position.Z - self.Z) * (radius / range);
        var length = offset.Length();
        if (length > edge && length > 0f)
            offset *= edge / length;
        return center + offset;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private static Vector2 HeadingScreen(float rotation) => new(MathF.Sin(rotation), MathF.Cos(rotation));

    private static Vector2 Rotate(Vector2 v, float radians)
    {
        var sin = MathF.Sin(radians);
        var cos = MathF.Cos(radians);
        return new Vector2(v.X * cos - v.Y * sin, v.X * sin + v.Y * cos);
    }
}
