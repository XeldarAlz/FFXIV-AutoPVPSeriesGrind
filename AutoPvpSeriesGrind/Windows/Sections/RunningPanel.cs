using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Modes;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class RunningPanel
{
    // Crystalline Conflict casual matches run 5 minutes.
    private const float MatchClockSeconds = 300f;

    private enum Display { Preparing, Queueing, Portraits, Fighting, InMatch, Finishing }

    private readonly record struct RingModel(float Fraction, string Big, string? Small, bool Endless);

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var s = ImGuiHelpers.GlobalScale;

        var inDuty = Svc.Condition[ConditionFlag.BoundByDuty];
        var inCombat = Svc.Condition[ConditionFlag.InCombat];
        var inQueue = Svc.Condition[ConditionFlag.InDutyQueue] || DutyOps.IsQueued();
        var timeLeft = inDuty ? DutyOps.ContentTimeLeft() : 0;

        var phase = ResolvePhase(ctrl, inDuty, inCombat, inQueue, timeLeft);
        var (accent, accentSoft, label) = Palette(phase);
        var finishing = phase == Display.Finishing;
        if (finishing)
        {
            accent = Styling.AccentAmber;
            accentSoft = Styling.AccentMint;
            label = "DONE";
        }

        Styling.VSpace(8);
        var labelCol = phase == Display.Fighting
            ? Styling.PulseColor(accent, accentSoft, Styling.PulseCalm)
            : accent;
        Styling.TextCentered($"·   {label}   ·", labelCol, 0.95f);
        Styling.VSpace(10);

        DrawRing(cfg, ctrl, phase, accent, accentSoft, finishing, radius: 58f, bigScale: 2.0f);
        Styling.VSpace(10);

        var activity = finishing
            ? (string.IsNullOrWhiteSpace(ctrl.Status) ? "Wrapping up the session" : ctrl.Status)
            : ActivityLine(ctrl, phase);
        DrawLiveLine(activity, finishing ? Styling.AccentMint : accentSoft);
        Styling.VSpace(12);

        if (!finishing)
        {
            if (phase == Display.Portraits)
                Styling.TextCentered($"Match starting in {Math.Max(timeLeft, 0)}s…", Styling.TextDim);
            else if (inDuty && timeLeft > 0 && phase is Display.Fighting or Display.InMatch)
                DrawMatchClock(timeLeft, accent, s);
            else if (!string.IsNullOrWhiteSpace(ctrl.Status))
                Styling.TextCentered(ctrl.Status, Styling.TextDim);
        }

        Styling.VSpace(8);
        DrawSessionStrip(cfg, ctrl);
        Styling.VSpace(16);

        var colW = MathF.Min(ImGui.GetContentRegionAvail().X, 320f * s);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetContentRegionAvail().X - colW) * 0.5f);
        if (PrimaryButton.Draw("STOP", Styling.AccentRose, true, colW))
            ctrl.Stop();
    }

    private static void DrawRing(Configuration cfg, AutoPvpSeriesController ctrl, Display phase,
        Vector4 accent, Vector4 accentSoft, bool finishing, float radius, float bigScale)
    {
        var s = ImGuiHelpers.GlobalScale;
        var model = ModelFor(cfg, ctrl);
        var r = radius * s;
        var thickness = MathF.Max(5f, radius * 0.12f) * s;

        var startScreen = ImGui.GetCursorScreenPos();
        var availX = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availX * 0.5f, startScreen.Y + r + 4f * s);

        var fighting = phase == Display.Fighting;
        var glow = 0.55f + (fighting ? 0.5f * Styling.Pulse(Styling.PulseCalm) : 0f) + (finishing ? 0.45f : 0f);
        ProgressRing.Glow(center, r, accent, glow);
        ProgressRing.Track(center, r, thickness, Styling.WithAlpha(Styling.BorderDim, 0.85f));

        if (finishing)
        {
            ProgressRing.Fill(center, r, thickness, 1f, accent);
            ProgressRing.Sweep(center, r, thickness * 0.55f, accentSoft, Styling.PulseOrbit, 1.0f, 0.40f);
            ProgressRing.CenterIcon(center, FontAwesomeIcon.Check, accent, r * 0.62f);
        }
        else if (model.Endless)
        {
            ProgressRing.Sweep(center, r, thickness, accentSoft, Styling.PulseOrbit, 2.4f, 0.95f);
            ProgressRing.CenterValue(center, model.Big, model.Small, Styling.TextStrong, accentSoft, bigScale);
        }
        else
        {
            ProgressRing.Fill(center, r, thickness, model.Fraction, accent);
            ProgressRing.Sweep(center, r, thickness * 0.5f, accentSoft, Styling.PulseOrbit, 0.85f, 0.25f);
            ProgressRing.CenterValue(center, model.Big, model.Small, Styling.TextStrong, Styling.TextDim, bigScale);
        }

        ImGui.SetCursorScreenPos(startScreen);
        ImGui.Dummy(new Vector2(availX, r * 2f + 8f * s));
    }

    private static RingModel ModelFor(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var matches = ctrl.SessionSnapshot?.MatchesCompleted ?? 0;
        switch (cfg.ActiveMode.Id)
        {
            case MatchCountMode.ModeId:
            {
                var target = Math.Max(1, cfg.TargetMatchCount);
                return new RingModel(matches / (float)target, matches.ToString(), $"/ {target}", false);
            }
            case TimeBoxedMode.ModeId:
            {
                var target = Math.Max(1, cfg.TargetMinutes);
                var mins = ctrl.SessionSnapshot?.Elapsed.TotalMinutes ?? 0;
                return new RingModel((float)(mins / target), ((int)mins).ToString(), $"/ {target}m", false);
            }
            case SeriesRankMode.ModeId:
            {
                var cur = PvpProfileReader.SeriesCurrentRank();
                return new RingModel(PvpProfileReader.SeriesRankProgress(), cur.ToString(), $"→ {cfg.TargetSeriesRank}", false);
            }
            default:
                return new RingModel(0f, matches.ToString(), "∞", true);
        }
    }

    private static void DrawLiveLine(string text, Vector4 accent)
    {
        var s = ImGuiHelpers.GlobalScale;
        var dotR = 3f * s;
        var gap = 7f * s;
        var lineH = ImGui.GetTextLineHeight();
        var textW = ImGui.CalcTextSize(text).X;
        var total = dotR * 2f + gap + textW;

        var leftX = ImGui.GetCursorPosX();
        var availX = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(leftX + MathF.Max(0f, (availX - total) * 0.5f));

        var p = ImGui.GetCursorScreenPos();
        var alpha = 0.35f + 0.65f * Styling.Pulse(Styling.PulseBreath);
        ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(p.X + dotR, p.Y + lineH * 0.5f), dotR, ImGui.GetColorU32(Styling.WithAlpha(accent, alpha)));

        ImGui.Dummy(new Vector2(dotR * 2f + gap, lineH));
        ImGui.SameLine(0, 0);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(text);
    }

    private static void DrawMatchClock(int timeLeft, Vector4 color, float s)
    {
        var frac = Math.Clamp(timeLeft / MatchClockSeconds, 0f, 1f);
        var w = MathF.Min(ImGui.GetContentRegionAvail().X, 280f * s);
        var h = 7f * s;

        var leftX = ImGui.GetCursorPosX();
        var availX = ImGui.GetContentRegionAvail().X;
        ImGui.SetCursorPosX(leftX + (availX - w) * 0.5f);

        var origin = ImGui.GetCursorScreenPos();
        var dl = ImGui.GetWindowDrawList();
        var end = origin + new Vector2(w, h);
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), h * 0.5f);
        if (frac > 0)
            dl.AddRectFilled(origin, new Vector2(origin.X + w * frac, end.Y), ImGui.GetColorU32(color * 0.9f), h * 0.5f);
        ImGui.Dummy(new Vector2(w, h));

        ImGui.SetCursorPosX(leftX);
        Styling.VSpace(2f);
        Styling.TextCentered($"{Formatting.Time(timeLeft)} left", Styling.TextDim);
    }

    private static void DrawSessionStrip(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var sess = ctrl.SessionSnapshot;
        var matches = sess?.MatchesCompleted ?? 0;
        var elapsed = sess?.Elapsed ?? TimeSpan.Zero;
        var rate = elapsed.TotalHours > 0 ? matches / elapsed.TotalHours : 0;
        var exp = sess?.SeriesExpGained ?? 0;

        var strip = cfg.ActiveMode.Id == MatchCountMode.ModeId
            ? $"{Formatting.Elapsed(elapsed)}     ·     {rate:F1}/h     ·     +{Formatting.Exp(exp)} Series"
            : $"{matches} matches     ·     {rate:F1}/h     ·     +{Formatting.Exp(exp)} Series";
        Styling.TextCentered(strip, Styling.TextDim);
    }

    private static Display ResolvePhase(AutoPvpSeriesController ctrl, bool inDuty, bool inCombat, bool inQueue, int timeLeft)
    {
        if (ctrl.Phase == AutoPhase.Finishing) return Display.Finishing;
        if (!inDuty) return inQueue ? Display.Queueing : Display.Preparing;
        if (timeLeft is > 1 and < 32) return Display.Portraits;
        if (inCombat) return Display.Fighting;
        return Display.InMatch;
    }

    private static (Vector4 accent, Vector4 accentSoft, string label) Palette(Display phase) => phase switch
    {
        Display.Preparing => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "STARTING"),
        Display.Queueing  => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "IN QUEUE"),
        Display.Portraits => (Styling.AccentAmber,  Styling.AccentAmberSoft,  "PORTRAITS"),
        Display.Fighting  => (Styling.AccentViolet, Styling.AccentVioletSoft, "FIGHTING"),
        Display.Finishing => (Styling.AccentMint,   Styling.AccentMintSoft,   "FINISHING UP"),
        _                 => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "IN MATCH"),
    };

    private static string ActivityLine(AutoPvpSeriesController ctrl, Display phase)
    {
        var job = ctrl.SessionSnapshot?.JobAbbr;
        var jobPrefix = string.IsNullOrEmpty(job) ? "" : $"{job}  ·  ";
        return phase switch
        {
            Display.Preparing => "Preparing to queue…",
            Display.Queueing  => "In queue for Casual Match…",
            Display.Portraits => $"Match starting  ·  {CurrentMapName()}",
            Display.Fighting  => $"{jobPrefix}{CurrentMapName()}",
            Display.Finishing => "Wrapping up the session",
            _                 => $"{jobPrefix}{CurrentMapName()}",
        };
    }

    private static string CurrentMapName()
    {
        var id = Svc.ClientState.TerritoryType;
        var name = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(id)?.PlaceName.ValueNullable?.Name.ExtractText();
        return string.IsNullOrEmpty(name) ? "the arena" : name;
    }
}
