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
    private const float MinimapMax = 300f;
    private const double PostureAnimMs = 420.0;
    private const double DestinationAnimMs = 520.0;
    private const float DestinationMoveThreshold = 3f;

    private const float BannerHeight = 40f;
    private const float BannerPad = 12f;
    private const float BannerIconBoxRatio = 0.5f;
    private const float BannerLabelGap = 12f;
    private const float BannerLabelScale = 1.25f;
    private const float BannerReasonScale = 0.95f;
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

    private const float LegendFontScale = 0.82f;
    private const float LegendGap = 14f;
    private const float HpBarHeightRatio = 0.85f;
    private const float StatGap = 6f;
    private const float StatTileHeight = 56f;
    private const string Dash = "--";

    private static readonly (string Text, Vector4 Color)[] Legend =
    {
        ("● enemy", Styling.AccentRose),
        ("◎ target", Styling.AccentVioletSoft),
        ("● ally", Styling.AccentMint),
        ("◆ point", Styling.AccentAmber),
    };

    private Posture lastPosture = Posture.Idle;
    private Vector3 lastDestination;
    private Transition postureChange;
    private Transition destinationChange;

    public BrainDebugWindow() : base("Auto PVP Series Grind: Brain###AutoPvpSeriesGrindBrain")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(420, 620);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var style = Styling.PushWindowStyle();
        var cfg = Plugin.Cfg;

        if (!cfg.EnableCombatBrain)
        {
            DrawContext(cfg, null);
            Styling.HairlineRule(3f, 10f);
            Styling.TextCentered("The combat brain is off.", Styling.TextSecondary);
            Styling.TextCentered("Turn it on in Settings → Combat.", Styling.TextMuted);
            return;
        }

        var inMatch = Plugin.Instance.Controller.Phase == AutoPhase.InMatch;
        if (BrainTelemetry.Plan is not { } plan || (!inMatch && !BrainTelemetry.IsFresh))
        {
            DrawContext(cfg, null);
            Styling.HairlineRule(3f, 12f);
            Styling.TextCentered(inMatch ? "Match starting: the brain spins up at the gate." : "Not in a live match.", Styling.TextMuted);
            return;
        }

        var snap = inMatch ? MatchState.Capture() : BrainTelemetry.Snapshot;
        if (snap is null) return;

        if (plan.Posture != lastPosture)
        {
            lastPosture = plan.Posture;
            postureChange.Mark();
        }

        if (plan.Kind != MoveKind.Hold && HorizontalDistance(plan.Destination, lastDestination) > DestinationMoveThreshold)
        {
            lastDestination = plan.Destination;
            destinationChange.Mark();
        }

        DrawContext(cfg, snap.SelfRole);
        Styling.HairlineRule(3f, 6f);
        DrawPostureBanner(plan, postureChange.Energy(PostureAnimMs));
        Styling.VSpace(8);
        DrawMinimap(snap, plan, destinationChange.Energy(DestinationAnimMs));
        Styling.VSpace(4);
        DrawLegend();
        Styling.VSpace(10);
        DrawHpBar(snap.SelfHp);
        Styling.VSpace(10);
        DrawStats(snap, plan);
    }

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
            Chip.Draw("vnavmesh offline · chat fallback", Styling.AccentRose);
        }
    }

    private static string PhaseLabel(AutoPhase phase) => phase switch
    {
        AutoPhase.Queueing => "in queue",
        AutoPhase.InMatch => "in match",
        AutoPhase.Finishing => "wrapping up",
        _ => "idle",
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
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Vector4.Lerp(Styling.CardBg, color, BannerTintRest + BannerTintFlash * energy)), Styling.CardRounding);
        dl.AddRectFilled(origin, new Vector2(origin.X + (BannerBarRest + BannerBarFlash * energy) * scale, end.Y), ImGui.GetColorU32(color), Styling.CardRounding, ImDrawFlags.RoundCornersLeft);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.WithAlpha(color, BannerBorderRest + BannerBorderFlash * energy)), Styling.CardRounding, ImDrawFlags.None, 1f);

        var iconBox = height * BannerIconBoxRatio;
        var iconCenter = new Vector2(origin.X + pad + iconBox * 0.5f, origin.Y + height * 0.5f);
        if (energy > 0f)
            ProgressRing.Glow(iconCenter, iconBox * (IconPingRadiusBase + IconPingRadiusSpread * (1f - energy)), color, IconPingIntensity * energy);
        ProgressRing.CenterIcon(iconCenter, PostureStyle.Icon(plan.Posture), Styling.WithAlpha(color, 1f - IconFadeEnergy * energy), iconBox * (1f + IconPopScale * energy));

        ImGui.SetWindowFontScale(BannerLabelScale);
        var labelSize = ImGui.CalcTextSize(label);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + pad + iconBox + (BannerLabelGap + LabelSlide * energy) * scale, origin.Y + (height - labelSize.Y) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.WithAlpha(color, 1f - LabelFadeEnergy * energy)))
            ImGui.TextUnformatted(label);
        ImGui.SetWindowFontScale(1f);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));

        if (!string.IsNullOrWhiteSpace(plan.Reason))
        {
            Styling.VSpace(3);
            Styling.TextCentered(plan.Reason, Styling.TextSecondary, BannerReasonScale);
        }
    }

    private static void DrawMinimap(PvpSnapshot snap, in MovePlan plan, float destinationEnergy)
    {
        var diameter = MathF.Min(ImGui.GetContentRegionAvail().X, MinimapMax * ImGuiHelpers.GlobalScale);
        BrainMinimap.Draw(snap, plan, diameter, destinationEnergy);
    }

    private static void DrawLegend()
    {
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.SetWindowFontScale(LegendFontScale);
        var gap = LegendGap * scale;
        var total = 0f;
        for (var partIndex = 0; partIndex < Legend.Length; partIndex++)
        {
            if (partIndex > 0)
                total += gap;
            total += ImGui.CalcTextSize(Legend[partIndex].Text).X;
        }
        Styling.CenterNextItem(total);
        for (var partIndex = 0; partIndex < Legend.Length; partIndex++)
        {
            if (partIndex > 0)
                ImGui.SameLine(0, gap);
            using (ImRaii.PushColor(ImGuiCol.Text, Legend[partIndex].Color))
                ImGui.TextUnformatted(Legend[partIndex].Text);
        }
        ImGui.SetWindowFontScale(1f);
    }

    private static void DrawHpBar(float hp)
    {
        var color = hp > HpHealthy ? Styling.AccentMint : hp > HpWounded ? Styling.AccentAmber : Styling.AccentRose;
        var width = ImGui.GetContentRegionAvail().X;
        var height = ImGui.GetFrameHeight() * HpBarHeightRatio;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(width, height);
        var dl = ImGui.GetWindowDrawList();

        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), height * 0.5f);
        if (hp > 0)
            dl.AddRectFilled(origin, new Vector2(origin.X + width * Math.Clamp(hp, 0f, 1f), end.Y), ImGui.GetColorU32(color), height * 0.5f);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.BorderDim), height * 0.5f);

        var text = $"HP {hp:P0}";
        var textSize = ImGui.CalcTextSize(text);
        ImGui.SetCursorScreenPos(new Vector2(origin.X + (width - textSize.X) * 0.5f, origin.Y + (height - textSize.Y) * 0.5f));
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(text);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
    }

    private static void DrawStats(PvpSnapshot snap, in MovePlan plan)
    {
        var tiles = new (FontAwesomeIcon Icon, string Value, string Caption, Vector4 Accent)[]
        {
            (FontAwesomeIcon.Skull, Distance(snap.Enemies.Count, snap.NearestEnemyDistance), "ENEMY", Styling.AccentRose),
            (FontAwesomeIcon.Users, Distance(snap.Allies.Count, snap.NearestAllyDistance), "ALLY", Styling.AccentMint),
            (FontAwesomeIcon.Gem, snap.HasObjective ? Yalms(HorizontalDistance(snap.Self, snap.Objective!.Value)) : Dash, "POINT", Styling.AccentAmber),
            (FontAwesomeIcon.Bullseye, TargetHp(snap, plan) is { } hp ? $"{hp * 100:F0}%" : Dash, "TARGET", Styling.AccentVioletSoft),
        };

        var scale = ImGuiHelpers.GlobalScale;
        var gap = StatGap * scale;
        var avail = ImGui.GetContentRegionAvail().X;
        var size = new Vector2((avail - gap * (tiles.Length - 1)) / tiles.Length, StatTileHeight * scale);

        for (var tileIndex = 0; tileIndex < tiles.Length; tileIndex++)
        {
            if (tileIndex > 0)
                ImGui.SameLine(0, gap);
            var tile = tiles[tileIndex];
            StatTile.Draw(tile.Icon, tile.Value, tile.Caption, tile.Accent, size);
        }
    }

    private static string Distance(int count, float nearest) => count == 0 ? Dash : Yalms(nearest);

    private static string Yalms(float distance) => $"{distance:F0}y";

    private static float? TargetHp(PvpSnapshot snap, in MovePlan plan)
    {
        if (plan.TargetId == 0)
            return null;
        for (var enemyIndex = 0; enemyIndex < snap.Enemies.Count; enemyIndex++)
            if (snap.Enemies[enemyIndex].Id == plan.TargetId)
                return snap.Enemies[enemyIndex].Hp;
        return null;
    }

    private static string RoleLabel(PvpRole role) => role switch
    {
        PvpRole.Tank => "tank",
        PvpRole.Melee => "melee",
        PvpRole.Ranged => "ranged",
        PvpRole.Healer => "healer",
        _ => "unknown role",
    };

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }
}
