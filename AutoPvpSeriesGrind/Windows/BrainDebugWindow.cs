using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed class BrainDebugWindow : Window, IDisposable
{
    private const float HpHealthy = 0.5f;
    private const float HpWounded = 0.25f;
    private const float MinimapMax = 300f;
    private const float PostureAnimMs = 420f;
    private const float DestinationAnimMs = 520f;
    private const float DestinationMoveThreshold = 3f;

    private const float BannerHeight = 44f;
    private const float BannerPad = 14f;
    private const float BannerIconBoxRatio = 0.5f;
    private const float BannerLabelGap = 12f;
    private const float BannerTintRest = 0.16f;
    private const float BannerTintFlash = 0.40f;
    private const float BannerBarRest = 3f;
    private const float BannerBarFlash = 5f;
    private const float BannerBorderRest = 0.45f;
    private const float BannerBorderFlash = 0.40f;
    private const float IconPingRadiusBase = 0.6f;
    private const float IconPingRadiusSpread = 1.4f;
    private const float IconPingIntensity = 1.6f;
    private const float IconPopScale = 0.22f;
    private const float IconFadeEnergy = 0.4f;
    private const float LabelSlide = 6f;
    private const float LabelFadeEnergy = 0.5f;

    private const float LegendGap = 14f;
    private const float HpBarHeight = 18f;
    private const float StatGap = 6f;
    private const float StatTileHeight = 62f;
    private const string Dash = "—";

    private static readonly (LocString Text, Vector4 Color)[] Legend =
    {
        (L.Brain.LegendEnemy, Styling.AccentRose),
        (L.Brain.LegendTarget, Styling.AccentVioletSoft),
        (L.Brain.LegendAlly, Styling.AccentMint),
        (L.Brain.LegendPoint, Styling.AccentAmber),
    };

    private Posture lastPosture = Posture.Idle;
    private Vector3 lastDestination;
    private long postureTick;
    private long destinationTick;
    private IDisposable? chrome;
    private IDisposable? bodyFont;

    public BrainDebugWindow() : base("Auto PVP Series Grind: Brain###AutoPvpSeriesGrindBrain")
    {
        Size = new Vector2(420, 660);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        bodyFont = Fonts.PushBody();
        chrome = Styling.PushChrome(new Vector2(16f, 14f));
    }

    public override void PostDraw()
    {
        chrome?.Dispose();
        chrome = null;
        bodyFont?.Dispose();
        bodyFont = null;
    }

    public override void Draw()
    {
        var cfg = Plugin.Cfg;

        if (!cfg.EnableCombatBrain)
        {
            DrawContext(cfg, null);
            Paint.Divider(8f);
            Styling.TextCentered(Loc.T(L.Brain.Off), Styling.TextSecondary);
            Styling.TextCentered(Loc.T(L.Brain.OffHint), Styling.TextMuted);
            return;
        }

        var inMatch = Plugin.Instance.Controller.Phase == AutoPhase.InMatch;
        if (BrainTelemetry.Plan is not { } plan || (!inMatch && !BrainTelemetry.IsFresh))
        {
            DrawContext(cfg, null);
            Paint.Divider(8f);
            Styling.TextCentered(Loc.T(inMatch ? L.Brain.MatchStarting : L.Brain.NotInMatch), Styling.TextMuted);
            return;
        }

        var snap = inMatch ? MatchState.Capture() : BrainTelemetry.Snapshot;
        if (snap is null) return;

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

        DrawContext(cfg, snap.SelfRole);
        Paint.Divider(6f);
        DrawPostureBanner(plan, Energy(postureTick, PostureAnimMs));
        Styling.VSpace(8);
        DrawMinimap(snap, plan, Energy(destinationTick, DestinationAnimMs));
        Styling.VSpace(6);
        DrawLegend();
        Styling.VSpace(10);
        DrawHpBar(snap.SelfHp);
        Styling.VSpace(10);
        DrawStats(snap, plan);
    }

    // Energy decays from 1 to 0 over the animation window, so a fresh change flashes and settles.
    private static float Energy(long markedTick, float durationMs)
        => markedTick == 0L ? 0f : 1f - Motion.Reveal(markedTick, durationMs);

    private static void DrawContext(Configuration cfg, PvpRole? role)
    {
        var gap = 6f * ImGuiHelpers.GlobalScale;
        var phase = Plugin.Instance.Controller.Phase;

        Chip.Draw(PhaseLabel(phase), PhaseColor(phase), dot: true, pulse: phase is AutoPhase.Queueing or AutoPhase.InMatch);
        ImGui.SameLine(0, gap);
        Chip.Draw(cfg.Strategy.ToString().ToLowerInvariant(), StrategyColor(cfg.Strategy));
        if (role is { } value)
        {
            ImGui.SameLine(0, gap);
            Chip.Draw(RoleLabel(value), Styling.TextDim);
        }

        if (!NavIpc.Instance.IsAvailable)
        {
            Styling.VSpace(4);
            Chip.Draw(Loc.T(L.Brain.NavFallback), Styling.AccentRose, FontAwesomeIcon.ExclamationTriangle);
        }
    }

    private static string PhaseLabel(AutoPhase phase) => phase switch
    {
        AutoPhase.Queueing => Loc.T(L.Brain.PhaseInQueue),
        AutoPhase.InMatch => Loc.T(L.Brain.PhaseInMatch),
        AutoPhase.Finishing => Loc.T(L.Brain.PhaseWrappingUp),
        _ => Loc.T(L.Brain.PhaseIdle),
    };

    private static Vector4 PhaseColor(AutoPhase phase) => phase switch
    {
        AutoPhase.Queueing => Styling.AccentAmber,
        AutoPhase.InMatch => Styling.AccentMint,
        AutoPhase.Finishing => Styling.AccentBlue,
        _ => Styling.TextMuted,
    };

    private static Vector4 StrategyColor(PvpStrategy strategy) => strategy switch
    {
        PvpStrategy.Defensive => Styling.AccentBlue,
        PvpStrategy.Aggressive => Styling.AccentRose,
        PvpStrategy.Custom => Styling.AccentMint,
        _ => Styling.AccentVioletSoft,
    };

    private static void DrawPostureBanner(in MovePlan plan, float energy)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var color = PostureStyle.Color(plan.Posture);
        var label = plan.Posture.ToString().ToUpperInvariant();
        var width = ImGui.GetContentRegionAvail().X;
        var height = BannerHeight * scale;
        var pad = BannerPad * scale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(width, height);
        var rounding = Styling.CardRounding * scale;
        var dl = ImGui.GetWindowDrawList();

        Paint.Fill(dl, origin, end, Vector4.Lerp(Styling.Surface1, color, BannerTintRest + BannerTintFlash * energy), rounding);
        Paint.Fill(dl, origin, new Vector2(origin.X + (BannerBarRest + BannerBarFlash * energy) * scale, end.Y), color, rounding, ImDrawFlags.RoundCornersLeft);
        Paint.TopLight(dl, origin, end, rounding);
        Paint.Stroke(dl, origin, end, Styling.WithAlpha(color, BannerBorderRest + BannerBorderFlash * energy), rounding);

        var iconBox = height * BannerIconBoxRatio;
        var iconCenter = new Vector2(origin.X + pad + iconBox * 0.5f, origin.Y + height * 0.5f);
        if (energy > 0f)
        {
            ProgressRing.Glow(iconCenter, iconBox * (IconPingRadiusBase + IconPingRadiusSpread * (1f - energy)), color, IconPingIntensity * energy);
        }

        ProgressRing.CenterIcon(iconCenter, PostureStyle.Icon(plan.Posture), Styling.WithAlpha(color, 1f - IconFadeEnergy * energy), iconBox * (1f + IconPopScale * energy));

        using (Fonts.PushHeadline())
        {
            var labelSize = TextDraw.Measure(label);
            TextDraw.At(label,
                new Vector2(origin.X + pad + iconBox + (BannerLabelGap + LabelSlide * energy) * scale, origin.Y + (height - labelSize.Y) * 0.5f),
                Styling.WithAlpha(color, 1f - LabelFadeEnergy * energy));
        }

        ImGui.Dummy(new Vector2(width, height));

        if (string.IsNullOrWhiteSpace(plan.Reason)) return;
        Styling.VSpace(4);
        using (Fonts.PushCaption())
            Styling.TextCentered(plan.Reason, Styling.TextSecondary);
    }

    private static void DrawMinimap(PvpSnapshot snap, in MovePlan plan, float destinationEnergy)
    {
        var diameter = MathF.Min(ImGui.GetContentRegionAvail().X, MinimapMax * ImGuiHelpers.GlobalScale);
        BrainMinimap.Draw(snap, plan, diameter, destinationEnergy);
    }

    private static void DrawLegend()
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var font = Fonts.PushCaption();
        var gap = LegendGap * scale;
        var origin = ImGui.GetCursorScreenPos();
        var avail = ImGui.GetContentRegionAvail().X;

        var total = 0f;
        for (var index = 0; index < Legend.Length; index++)
        {
            if (index > 0) total += gap;
            total += TextDraw.Measure(Loc.T(Legend[index].Text)).X;
        }

        var x = origin.X + MathF.Max(0f, (avail - total) * 0.5f);
        var lineHeight = ImGui.GetTextLineHeight();
        for (var index = 0; index < Legend.Length; index++)
        {
            var entry = Loc.T(Legend[index].Text);
            TextDraw.At(entry, new Vector2(x, origin.Y), Legend[index].Color);
            x += TextDraw.Measure(entry).X + gap;
        }

        ImGui.Dummy(new Vector2(avail, lineHeight));
    }

    private static void DrawHpBar(float hp)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var color = hp > HpHealthy ? Styling.AccentMint : hp > HpWounded ? Styling.AccentAmber : Styling.AccentRose;
        var width = ImGui.GetContentRegionAvail().X;
        var height = HpBarHeight * scale;
        var origin = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();

        Paint.Bar(dl, origin, width, height, hp, color);

        var text = $"HP {hp:P0}";
        var textSize = TextDraw.Measure(text);
        TextDraw.At(text, new Vector2(origin.X + (width - textSize.X) * 0.5f, origin.Y + (height - textSize.Y) * 0.5f), Styling.TextStrong);

        ImGui.Dummy(new Vector2(width, height));
    }

    private static void DrawStats(PvpSnapshot snap, in MovePlan plan)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var gap = StatGap * scale;
        var avail = ImGui.GetContentRegionAvail().X;
        var tileWidth = (avail - gap * 3f) / 4f;

        StatTile.Draw(Loc.T(L.Brain.Enemy), Distance(snap.Enemies.Count, snap.NearestEnemyDistance), null, Styling.AccentRose, tileWidth, StatTileHeight);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.Brain.Ally), Distance(snap.Allies.Count, snap.NearestAllyDistance), null, Styling.AccentMint, tileWidth, StatTileHeight);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.Brain.Point), snap.HasObjective ? Yalms(HorizontalDistance(snap.Self, snap.Objective!.Value)) : Dash, null, Styling.AccentAmber, tileWidth, StatTileHeight);
        ImGui.SameLine(0, gap);
        StatTile.Draw(Loc.T(L.Brain.Target), TargetHp(snap, plan) is { } hp ? $"{hp * 100:F0}%" : Dash, null, Styling.AccentVioletSoft, tileWidth, StatTileHeight);
    }

    private static string Distance(int count, float nearest) => count == 0 ? Dash : Yalms(nearest);

    private static string Yalms(float distance) => $"{distance:F0}y";

    private static float? TargetHp(PvpSnapshot snap, in MovePlan plan)
    {
        if (plan.TargetId == 0) return null;
        for (var enemyIndex = 0; enemyIndex < snap.Enemies.Count; enemyIndex++)
        {
            if (snap.Enemies[enemyIndex].Id == plan.TargetId) return snap.Enemies[enemyIndex].Hp;
        }

        return null;
    }

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
