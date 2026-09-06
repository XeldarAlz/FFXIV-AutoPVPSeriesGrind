using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class BrainPanel
{
    private const float Pad = 18f;
    private const float RadarDiameter = 236f;
    private const float RadarGap = 24f;
    private const float LegendGap = 8f;
    private const float LegendItemGap = 14f;
    private const float MinColumnWidth = 300f;
    private const float RowGap = 10f;
    private const float GroupGap = 16f;
    private const float RevealMs = 320f;
    private const float RevealSlide = 10f;
    private const long AppearGapMs = 500;

    private const float TintAmount = 0.07f;
    private const float BorderThickness = 1.4f;
    private const float BorderRestAlpha = 0.45f;
    private const float BorderFlashAlpha = 0.35f;

    private const float TitleDotRadius = 3.5f;
    private const float TitleDotGap = 10f;
    private const float TitleChipGap = 12f;
    private const float ChipPadX = 9f;
    private const float ChipPadY = 3f;
    private const float ChipGap = 6f;
    private const float ChipIconGap = 6f;
    private const float ChipFillAlpha = 0.16f;
    private const float ChipBorderAlpha = 0.45f;

    private const float PostureDisc = 38f;
    private const float PostureIconRatio = 0.5f;
    private const float PostureLabelGap = 12f;
    private const float PostureAnimMs = 420f;
    private const float DestinationAnimMs = 520f;
    private const float DestinationMoveThreshold = 3f;
    private const float PingRadiusBase = 0.6f;
    private const float PingRadiusSpread = 1.4f;
    private const float PingIntensity = 1.6f;
    private const float DiscFillAlpha = 0.18f;
    private const float DiscRingThickness = 1.2f;
    private const float DiscRingAlpha = 0.6f;
    private const float IconPopScale = 0.2f;
    private const float LabelSlide = 6f;
    private const float LabelFadeEnergy = 0.5f;
    private const float SprintGap = 10f;

    private const int ReasonMaxLines = 2;

    private const float HpHealthy = 0.5f;
    private const float HpWounded = 0.25f;
    private const float HpBarHeight = 10f;
    private const float HpBarGap = 6f;
    private const float HpApproachSpeed = 10f;

    private const int ReadoutCount = 4;
    private const float ReadoutInset = 12f;
    private const float ReadoutDotRadius = 3f;
    private const float ReadoutLabelGap = 4f;
    private const float ReadoutSubGap = 6f;
    private const float ReadoutRuleAlpha = 0.55f;

    private const string Dash = "–";
    private const string HpLabel = "HP";

    private static readonly (LocString Text, Vector4 Color)[] Legend =
    {
        (L.Brain.LegendEnemy, Styling.AccentRose),
        (L.Brain.LegendTarget, Styling.AccentVioletSoft),
        (L.Brain.LegendAlly, Styling.AccentMint),
        (L.Brain.LegendPoint, Styling.AccentAmber),
    };

    private static Posture lastPosture = Posture.Idle;
    private static Vector3 lastDestination;
    private static long postureTick;
    private static long destinationTick;
    private static long lastDrawTick;
    private static long shownTick;

    private readonly record struct Metrics(float Caption, float Headline, float Body, float Chip, float Reason)
    {
        public float Posture(float scale) => MathF.Max(PostureDisc * scale, Headline);

        public float Hp(float scale) => Caption + (HpBarGap + HpBarHeight) * scale;

        public float Readouts(float scale) => Caption + ReadoutLabelGap * scale + Headline;

        public float Column(float scale)
            => Chip + RowGap * scale
             + Posture(scale) + RowGap * scale
             + Reason + GroupGap * scale
             + Hp(scale) + GroupGap * scale
             + Readouts(scale);
    }

    public static bool Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        if (!cfg.EnableCombatBrain || ctrl.Phase != AutoPhase.InMatch || BrainTelemetry.Plan is not { } plan) return false;

        var snap = MatchState.Capture();
        TrackAppearance();
        TrackChanges(plan);

        using var reveal = Motion.PushReveal(Motion.Reveal(shownTick, RevealMs), RevealSlide);
        DrawCard(cfg, snap, plan);
        return true;
    }

    private static void TrackAppearance()
    {
        var now = Environment.TickCount64;
        if (now - lastDrawTick > AppearGapMs) shownTick = now;
        lastDrawTick = now;
    }

    private static void TrackChanges(in MovePlan plan)
    {
        if (plan.Posture != lastPosture)
        {
            lastPosture = plan.Posture;
            postureTick = Environment.TickCount64;
        }

        if (plan.Kind != MoveKind.Hold && HorizontalDistance(plan.Destination, lastDestination) > DestinationMoveThreshold)
        {
            lastDestination = plan.Destination;
            destinationTick = Environment.TickCount64;
        }
    }

    // Energy decays from 1 to 0 over the animation window, so a fresh change flashes and settles.
    private static float Energy(long markedTick, float durationMs)
        => markedTick == 0L ? 0f : 1f - Motion.Reveal(markedTick, durationMs);

    private static void DrawCard(Configuration cfg, PvpSnapshot snap, in MovePlan plan)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var origin = ImGui.GetCursorScreenPos();
        var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
        var pad = Pad * scale;
        var inner = MathF.Max(1f, width - pad * 2f);
        var stacked = inner < (RadarDiameter + RadarGap + MinColumnWidth) * scale;
        var diameter = stacked ? MathF.Min(RadarDiameter * scale, inner) : RadarDiameter * scale;
        var columnWidth = stacked ? inner : inner - diameter - RadarGap * scale;
        var metrics = Measure(plan.Reason, columnWidth, scale);
        var radarBlock = diameter + LegendGap * scale + metrics.Caption;
        var columnHeight = metrics.Column(scale);
        var height = stacked
            ? pad * 2f + radarBlock + GroupGap * scale + columnHeight
            : pad * 2f + MathF.Max(radarBlock, columnHeight);
        var end = origin + new Vector2(width, height);

        var color = PostureStyle.Color(plan.Posture);
        var postureEnergy = Energy(postureTick, PostureAnimMs);
        var rounding = Styling.PanelRounding * scale;
        Paint.Glass(dl, origin, end, rounding, color, TintAmount);
        Paint.Stroke(dl, origin, end, Styling.WithAlpha(color, BorderRestAlpha + BorderFlashAlpha * postureEnergy), rounding, BorderThickness);

        dl.PushClipRect(origin, end, true);

        var radarCenter = stacked
            ? new Vector2(origin.X + width * 0.5f, origin.Y + pad + diameter * 0.5f)
            : new Vector2(origin.X + pad + diameter * 0.5f, origin.Y + pad + (height - pad * 2f - radarBlock) * 0.5f + diameter * 0.5f);
        BrainMinimap.Draw(snap, plan, radarCenter, diameter, Energy(destinationTick, DestinationAnimMs));
        DrawLegend(radarCenter.X, radarCenter.Y + diameter * 0.5f + LegendGap * scale);

        var columnMin = stacked
            ? new Vector2(origin.X + pad, origin.Y + pad + radarBlock + GroupGap * scale)
            : new Vector2(origin.X + pad + diameter + RadarGap * scale, origin.Y + pad);
        DrawColumn(cfg, snap, plan, columnMin, columnWidth, end.Y - pad, metrics, color, postureEnergy);

        dl.PopClipRect();

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
    }

    private static Metrics Measure(string reason, float columnWidth, float scale)
    {
        float caption;
        float headline;
        using (Fonts.PushCaption()) caption = ImGui.GetTextLineHeight();
        using (Fonts.PushHeadline()) headline = ImGui.GetTextLineHeight();

        var reasonHeight = caption;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            using (Fonts.PushCaption())
                reasonHeight = Math.Clamp(TextDraw.MeasureWrapped(reason, MathF.Max(1f, columnWidth)).Y, caption, caption * ReasonMaxLines);
        }

        return new Metrics(caption, headline, ImGui.GetTextLineHeight(), caption + ChipPadY * 2f * scale, reasonHeight);
    }

    private static void DrawLegend(float centerX, float y)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var font = Fonts.PushCaption();
        var gap = LegendItemGap * scale;

        var total = 0f;
        for (var index = 0; index < Legend.Length; index++)
        {
            if (index > 0) total += gap;
            total += TextDraw.Measure(Loc.T(Legend[index].Text)).X;
        }

        var x = centerX - total * 0.5f;
        for (var index = 0; index < Legend.Length; index++)
        {
            var entry = Loc.T(Legend[index].Text);
            TextDraw.At(entry, new Vector2(x, y), Legend[index].Color);
            x += TextDraw.Measure(entry).X + gap;
        }
    }

    private static void DrawColumn(Configuration cfg, PvpSnapshot snap, in MovePlan plan, Vector2 min, float width, float bottom,
        in Metrics metrics, Vector4 color, float energy)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var x = min.X;
        var y = min.Y;

        DrawTitleRow(cfg, snap, x, y, width, metrics, color);
        y += metrics.Chip + RowGap * scale;

        DrawPosture(plan, x, y, width, metrics, color, energy);
        y += metrics.Posture(scale) + RowGap * scale;

        DrawReason(plan.Reason, x, y, width, metrics.Reason);
        y += metrics.Reason;

        var hpHeight = metrics.Hp(scale);
        var groupTop = MathF.Max(y + GroupGap * scale, bottom - hpHeight - GroupGap * scale - metrics.Readouts(scale));
        DrawHp(snap.SelfHp, x, groupTop, width, metrics);
        DrawReadouts(snap, plan, x, groupTop + hpHeight + GroupGap * scale, width, metrics);
    }

    private static void DrawTitleRow(Configuration cfg, PvpSnapshot snap, float x, float y, float width, in Metrics metrics, Vector4 color)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var midY = y + metrics.Chip * 0.5f;
        var dotRadius = TitleDotRadius * scale;

        Paint.Dot(dl, new Vector2(x + dotRadius, midY), dotRadius, Styling.PulseColor(color, Styling.Lighten(color, 0.35f), Styling.PulseMedium));

        var title = Loc.T(L.Brain.Title);
        var titleSize = TextDraw.SmallCapsSize(title);
        var titleX = x + dotRadius * 2f + TitleDotGap * scale;
        TextDraw.SmallCaps(title, new Vector2(titleX, midY - titleSize.Y * 0.5f), Styling.TextSecondary);

        var leftLimit = titleX + titleSize.X + TitleChipGap * scale;
        var rightX = x + width;
        if (!NavIpc.Instance.IsAvailable)
        {
            rightX = DrawChip(dl, rightX, y, leftLimit, Loc.T(L.Brain.NavFallback), Styling.AccentRose, FontAwesomeIcon.ExclamationTriangle, metrics.Chip);
        }

        rightX = DrawChip(dl, rightX, y, leftLimit, StrategyLabel(cfg.Strategy), StrategyColor(cfg.Strategy), null, metrics.Chip);
        DrawChip(dl, rightX, y, leftLimit, RoleLabel(snap.SelfRole), Styling.TextDim, null, metrics.Chip);
    }

    private static float DrawChip(ImDrawListPtr dl, float rightX, float y, float leftLimit, string label, Vector4 accent, FontAwesomeIcon? icon, float height)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var font = Fonts.PushCaption();
        var textSize = TextDraw.Measure(label);
        var iconWidth = icon is { } glyph ? TextDraw.IconSize(glyph).X + ChipIconGap * scale : 0f;
        var chipWidth = ChipPadX * 2f * scale + iconWidth + textSize.X;
        var minX = rightX - chipWidth;
        if (minX < leftLimit) return rightX;

        Paint.Pill(dl, new Vector2(minX, y), new Vector2(rightX, y + height), Styling.WithAlpha(accent, ChipFillAlpha), Styling.WithAlpha(accent, ChipBorderAlpha));

        var midY = y + height * 0.5f;
        var contentX = minX + ChipPadX * scale;
        if (icon is { } iconGlyph)
        {
            var iconSize = TextDraw.IconSize(iconGlyph);
            TextDraw.Icon(iconGlyph, new Vector2(contentX, midY - iconSize.Y * 0.5f), accent);
            contentX += iconSize.X + ChipIconGap * scale;
        }

        TextDraw.At(label, new Vector2(contentX, midY - textSize.Y * 0.5f), Styling.TextSecondary);
        return minX - ChipGap * scale;
    }

    private static void DrawPosture(in MovePlan plan, float x, float y, float width, in Metrics metrics, Vector4 color, float energy)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var disc = PostureDisc * scale;
        var rowHeight = metrics.Posture(scale);
        var center = new Vector2(x + disc * 0.5f, y + rowHeight * 0.5f);

        if (energy > 0f)
        {
            ProgressRing.Glow(center, disc * 0.5f * (PingRadiusBase + PingRadiusSpread * (1f - energy)), color, PingIntensity * energy);
        }

        dl.AddCircleFilled(center, disc * 0.5f, Paint.Col(Styling.WithAlpha(color, DiscFillAlpha)));
        ProgressRing.Track(center, disc * 0.5f, DiscRingThickness * scale, Styling.WithAlpha(color, DiscRingAlpha));
        ProgressRing.CenterIcon(center, PostureStyle.Icon(plan.Posture), color, disc * PostureIconRatio * (1f + IconPopScale * energy));

        var labelX = x + disc + (PostureLabelGap + LabelSlide * energy) * scale;
        var rightX = x + width;
        if (plan.Sprint && plan.Kind != MoveKind.Hold)
        {
            rightX = DrawSprint(rightX, center.Y) - SprintGap * scale;
        }

        using (Fonts.PushHeadline())
        {
            var label = TextDraw.Truncate(TextDraw.Upper(PostureLabel(plan.Posture)), rightX - labelX);
            var labelSize = TextDraw.Measure(label);
            TextDraw.At(label, new Vector2(labelX, center.Y - labelSize.Y * 0.5f), Styling.WithAlpha(color, 1f - LabelFadeEnergy * energy));
        }
    }

    private static float DrawSprint(float rightX, float midY)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var font = Fonts.PushCaption();
        var label = Loc.T(L.Brain.Sprinting);
        var labelSize = TextDraw.Measure(label);
        var iconSize = TextDraw.IconSize(FontAwesomeIcon.Bolt);
        var labelX = rightX - labelSize.X;
        var iconX = labelX - ChipIconGap * scale - iconSize.X;
        TextDraw.Icon(FontAwesomeIcon.Bolt, new Vector2(iconX, midY - iconSize.Y * 0.5f), Styling.AccentAmberSoft);
        TextDraw.At(label, new Vector2(labelX, midY - labelSize.Y * 0.5f), Styling.TextDim);
        return iconX;
    }

    private static void DrawReason(string reason, float x, float y, float width, float height)
    {
        if (string.IsNullOrWhiteSpace(reason)) return;

        var dl = ImGui.GetWindowDrawList();
        using var font = Fonts.PushCaption();
        dl.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);
        TextDraw.Wrapped(reason, new Vector2(x, y), MathF.Max(1f, width), Styling.TextSecondary);
        dl.PopClipRect();
    }

    private static void DrawHp(float hp, float x, float y, float width, in Metrics metrics)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var color = hp > HpHealthy ? Styling.AccentMint : hp > HpWounded ? Styling.AccentAmber : Styling.AccentRose;

        TextDraw.SmallCaps(HpLabel, new Vector2(x, y), Styling.TextDim);
        TextDraw.Right(Percent(hp), x + width, y + (metrics.Caption - metrics.Body) * 0.5f, Styling.TextStrong);

        var shown = Motion.Approach(Motion.Key("##apsg_brain_hp"), hp, HpApproachSpeed);
        Paint.Bar(dl, new Vector2(x, y + metrics.Caption + HpBarGap * scale), width, HpBarHeight * scale, shown, color);
    }

    private static void DrawReadouts(PvpSnapshot snap, in MovePlan plan, float x, float y, float width, in Metrics metrics)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var columnWidth = width / ReadoutCount;
        var height = metrics.Readouts(scale);
        var rule = Styling.WithAlpha(Styling.BorderDim, ReadoutRuleAlpha);

        for (var index = 1; index < ReadoutCount; index++)
        {
            var ruleX = x + columnWidth * index;
            dl.AddLine(new Vector2(ruleX, y), new Vector2(ruleX, y + height), Paint.Col(rule), 1f);
        }

        var targetHp = TargetHp(snap, plan);
        DrawReadout(x, y, columnWidth, metrics, 0, Loc.T(L.Brain.Enemy), Nearest(snap.Enemies.Count, snap.NearestEnemyDistance), Count(snap.Enemies.Count), Styling.AccentRose);
        DrawReadout(x, y, columnWidth, metrics, 1, Loc.T(L.Brain.Ally), Nearest(snap.Allies.Count, snap.NearestAllyDistance), Count(snap.Allies.Count), Styling.AccentMint);
        DrawReadout(x, y, columnWidth, metrics, 2, Loc.T(L.Brain.Point), snap.Objective is { } objective ? Yalms(HorizontalDistance(snap.Self, objective)) : Dash, null, Styling.AccentAmber);
        DrawReadout(x, y, columnWidth, metrics, 3, Loc.T(L.Brain.Target), targetHp is { } value ? Percent(value) : Dash, null, Styling.AccentVioletSoft);
    }

    private static void DrawReadout(float x, float y, float columnWidth, in Metrics metrics, int index, string label, string value, string? sub, Vector4 accent)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dl = ImGui.GetWindowDrawList();
        var inset = index == 0 ? 0f : ReadoutInset * scale;
        var left = x + columnWidth * index + inset;
        var right = x + columnWidth * (index + 1) - (index == ReadoutCount - 1 ? 0f : ReadoutInset * scale);
        var avail = right - left;
        var dotRadius = ReadoutDotRadius * scale;

        dl.AddCircleFilled(new Vector2(left + dotRadius, y + metrics.Caption * 0.5f), dotRadius, Paint.Col(accent));
        var labelX = left + dotRadius * 2f + ChipIconGap * scale;
        using (Fonts.PushCaption())
            TextDraw.At(TextDraw.Truncate(TextDraw.Upper(label), right - labelX), new Vector2(labelX, y), Styling.TextDim);

        var valueY = y + metrics.Caption + ReadoutLabelGap * scale;
        float valueWidth;
        using (Fonts.PushHeadline())
        {
            var shown = TextDraw.Truncate(value, avail);
            valueWidth = TextDraw.Measure(shown).X;
            TextDraw.At(shown, new Vector2(left, valueY), Styling.TextStrong);
        }

        if (sub is null) return;
        using (Fonts.PushCaption())
        {
            var subX = left + valueWidth + ReadoutSubGap * scale;
            var subShown = TextDraw.Truncate(sub, right - subX);
            TextDraw.At(subShown, new Vector2(subX, valueY + metrics.Headline - metrics.Caption - 1f * scale), Styling.TextDim);
        }
    }

    private static string Nearest(int count, float distance) => count == 0 ? Dash : Yalms(distance);

    private static string? Count(int count) => count == 0 ? null : $"×{count}";

    private static string Yalms(float distance) => $"{distance:F0}y";

    private static string Percent(float fraction) => $"{(int)MathF.Round(fraction * 100f)}%";

    private static float? TargetHp(PvpSnapshot snap, in MovePlan plan)
    {
        if (plan.TargetId == 0) return null;
        for (var enemyIndex = 0; enemyIndex < snap.Enemies.Count; enemyIndex++)
        {
            if (snap.Enemies[enemyIndex].Id == plan.TargetId) return snap.Enemies[enemyIndex].Hp;
        }

        return null;
    }

    private static string PostureLabel(Posture posture) => posture switch
    {
        Posture.Hold => Loc.T(L.Brain.PostureHold),
        Posture.Push => Loc.T(L.Brain.PosturePush),
        Posture.Stage => Loc.T(L.Brain.PostureStage),
        Posture.Reposition => Loc.T(L.Brain.PostureReposition),
        Posture.Regroup => Loc.T(L.Brain.PostureRegroup),
        Posture.Retreat => Loc.T(L.Brain.PostureRetreat),
        _ => Loc.T(L.Brain.PostureIdle),
    };

    private static string StrategyLabel(PvpStrategy strategy) => strategy switch
    {
        PvpStrategy.Defensive => Loc.T(L.Settings.StrategyDefensive),
        PvpStrategy.Aggressive => Loc.T(L.Settings.StrategyAggressive),
        PvpStrategy.Custom => Loc.T(L.Settings.StrategyCustom),
        _ => Loc.T(L.Settings.StrategyModerate),
    };

    private static Vector4 StrategyColor(PvpStrategy strategy) => strategy switch
    {
        PvpStrategy.Defensive => Styling.AccentBlue,
        PvpStrategy.Aggressive => Styling.AccentRose,
        PvpStrategy.Custom => Styling.AccentMint,
        _ => Styling.AccentVioletSoft,
    };

    private static string RoleLabel(PvpRole role) => role switch
    {
        PvpRole.Tank => Loc.T(L.Brain.RoleTank),
        PvpRole.Melee => Loc.T(L.Brain.RoleMelee),
        PvpRole.Ranged => Loc.T(L.Brain.RoleRanged),
        PvpRole.Healer => Loc.T(L.Brain.RoleHealer),
        _ => Loc.T(L.Brain.RoleUnknown),
    };

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}
