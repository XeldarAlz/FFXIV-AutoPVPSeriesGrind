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
    private const float MatchClockSeconds = Core.ApsgConstants.CrystallineConflict.MatchLengthSec;

    private enum Display { Preparing, Queueing, Portraits, Fighting, InMatch, Finishing }

    private readonly record struct RingModel(float Fraction, string Big, string? Small, bool Endless);

    private static uint cachedTerritoryId;
    private static string? cachedMapName;

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;

        var inDuty = Svc.Condition[ConditionFlag.BoundByDuty];
        var inCombat = Svc.Condition[ConditionFlag.InCombat];
        var inQueue = Svc.Condition[ConditionFlag.InDutyQueue] || DutyOps.IsQueued();
        var timeLeft = inDuty ? DutyOps.ContentTimeLeft() : 0;

        var phase = ResolvePhase(ctrl, inDuty, inCombat, inQueue, timeLeft);
        var (accent, accentSoft, label) = Palette(phase);

        Styling.VSpace(8);
        DrawStatusLabel(phase, accent, accentSoft, label);
        Styling.VSpace(10);

        DrawRing(cfg, ctrl, phase, accent, accentSoft, radius: Layout.HeroRingRadius, bigScale: 2.0f);
        Styling.VSpace(10);

        DrawActivitySubline(ctrl, phase, accentSoft);
        Styling.VSpace(12);

        DrawClockOrStatus(ctrl, phase, accent, inDuty, timeLeft, scale);

        Styling.VSpace(8);
        DrawSessionStrip(cfg, ctrl);
        Styling.VSpace(16);

        DrawStopButton(ctrl, scale);
    }

    private static void DrawStatusLabel(Display phase, Vector4 accent, Vector4 accentSoft, string label)
    {
        var labelColor = phase == Display.Fighting
            ? Styling.PulseColor(accent, accentSoft, Styling.PulseCalm)
            : accent;
        Styling.TextCentered($"·   {label}   ·", labelColor, 0.95f);
    }

    private static void DrawActivitySubline(AutoPvpSeriesController ctrl, Display phase, Vector4 accentSoft)
    {
        var activity = phase == Display.Finishing
            ? (string.IsNullOrWhiteSpace(ctrl.Status) ? "Wrapping up the session" : ctrl.Status)
            : ActivityLine(ctrl, phase);
        DrawLiveLine(activity, accentSoft);
    }

    private static void DrawClockOrStatus(AutoPvpSeriesController ctrl, Display phase, Vector4 accent,
        bool inDuty, int timeLeft, float scale)
    {
        if (phase == Display.Finishing)
        {
            return;
        }

        if (phase == Display.Portraits)
        {
            Styling.TextCentered($"Match starting in {Math.Max(timeLeft, 0)}s…", Styling.TextDim);
        }
        else if (inDuty && timeLeft > 0 && phase is Display.Fighting or Display.InMatch)
        {
            DrawMatchClock(timeLeft, accent, scale);
        }
        else if (!string.IsNullOrWhiteSpace(ctrl.Status))
        {
            Styling.TextCentered(ctrl.Status, Styling.TextDim);
        }
    }

    private static void DrawStopButton(AutoPvpSeriesController ctrl, float scale)
    {
        var columnWidth = MathF.Min(ImGui.GetContentRegionAvail().X, 320f * scale);
        Styling.CenterNextItem(columnWidth);
        if (PrimaryButton.Draw("STOP", Styling.AccentRose, true, columnWidth))
        {
            ctrl.Stop();
        }
    }

    private static void DrawRing(Configuration cfg, AutoPvpSeriesController ctrl, Display phase,
        Vector4 accent, Vector4 accentSoft, float radius, float bigScale)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var model = ModelFor(cfg, ctrl);
        var scaledRadius = radius * scale;
        var thickness = MathF.Max(5f, radius * 0.12f) * scale;

        var startScreen = ImGui.GetCursorScreenPos();
        var availableWidth = ImGui.GetContentRegionAvail().X;
        var center = new Vector2(startScreen.X + availableWidth * 0.5f, startScreen.Y + scaledRadius + 4f * scale);

        var finishing = phase == Display.Finishing;
        var fighting = phase == Display.Fighting;
        var glow = 0.55f + (fighting ? 0.5f * Styling.Pulse(Styling.PulseCalm) : 0f) + (finishing ? 0.45f : 0f);
        ProgressRing.Glow(center, scaledRadius, accent, glow);
        ProgressRing.Track(center, scaledRadius, thickness, Layout.RingTrackColor);

        if (finishing)
        {
            ProgressRing.Fill(center, scaledRadius, thickness, 1f, accent);
            ProgressRing.Sweep(center, scaledRadius, thickness * 0.55f, accentSoft, Styling.PulseOrbit, 1.0f, 0.40f);
            ProgressRing.CenterIcon(center, FontAwesomeIcon.Check, accent, scaledRadius * Layout.HeroRingIconRatio);
        }
        else if (model.Endless)
        {
            ProgressRing.Sweep(center, scaledRadius, thickness, accentSoft, Styling.PulseOrbit, 2.4f, 0.95f);
            ProgressRing.CenterValue(center, model.Big, model.Small, Styling.TextStrong, accentSoft, bigScale);
        }
        else
        {
            ProgressRing.Fill(center, scaledRadius, thickness, model.Fraction, accent);
            ProgressRing.Sweep(center, scaledRadius, thickness * 0.5f, accentSoft, Styling.PulseOrbit, 0.85f, 0.25f);
            ProgressRing.CenterValue(center, model.Big, model.Small, Styling.TextStrong, Styling.TextDim, bigScale);
        }

        ImGui.SetCursorScreenPos(startScreen);
        ImGui.Dummy(new Vector2(availableWidth, scaledRadius * 2f + 8f * scale));
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
                var elapsedMinutes = ctrl.SessionSnapshot?.Elapsed.TotalMinutes ?? 0;
                return new RingModel((float)(elapsedMinutes / target), ((int)elapsedMinutes).ToString(), $"/ {target}m", false);
            }
            case SeriesRankMode.ModeId:
            {
                var currentRank = PvpProfileReader.SeriesCurrentRank();
                return new RingModel(PvpProfileReader.SeriesRankProgress(), currentRank.ToString(), $"→ {cfg.TargetSeriesRank}", false);
            }
            default:
                return new RingModel(0f, matches.ToString(), "∞", true);
        }
    }

    private static void DrawLiveLine(string text, Vector4 accent)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var dotRadius = 3f * scale;
        var gap = 7f * scale;
        var lineHeight = ImGui.GetTextLineHeight();
        var textWidth = ImGui.CalcTextSize(text).X;
        var totalWidth = dotRadius * 2f + gap + textWidth;

        Styling.CenterNextItem(totalWidth);

        var cursorScreen = ImGui.GetCursorScreenPos();
        var alpha = 0.35f + 0.65f * Styling.Pulse(Styling.PulseBreath);
        ImGui.GetWindowDrawList().AddCircleFilled(
            new Vector2(cursorScreen.X + dotRadius, cursorScreen.Y + lineHeight * 0.5f), dotRadius, ImGui.GetColorU32(Styling.WithAlpha(accent, alpha)));

        ImGui.Dummy(new Vector2(dotRadius * 2f + gap, lineHeight));
        ImGui.SameLine(0, 0);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
        {
            ImGui.TextUnformatted(text);
        }
    }

    private static void DrawMatchClock(int timeLeft, Vector4 color, float scale)
    {
        var fraction = Math.Clamp(timeLeft / MatchClockSeconds, 0f, 1f);
        var barWidth = MathF.Min(ImGui.GetContentRegionAvail().X, 280f * scale);
        var barHeight = 7f * scale;

        var leftX = ImGui.GetCursorPosX();
        Styling.CenterNextItem(barWidth);

        var origin = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var end = origin + new Vector2(barWidth, barHeight);
        drawList.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), barHeight * 0.5f);
        if (fraction > 0)
        {
            drawList.AddRectFilled(origin, new Vector2(origin.X + barWidth * fraction, end.Y), ImGui.GetColorU32(color * 0.9f), barHeight * 0.5f);
        }

        ImGui.Dummy(new Vector2(barWidth, barHeight));

        ImGui.SetCursorPosX(leftX);
        Styling.VSpace(2f);
        Styling.TextCentered($"{Formatting.Time(timeLeft)} left", Styling.TextDim);
    }

    private static void DrawSessionStrip(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var session = ctrl.SessionSnapshot;
        var matches = session?.MatchesCompleted ?? 0;
        var elapsed = session?.Elapsed ?? TimeSpan.Zero;
        var rate = elapsed.TotalHours > 0 ? matches / elapsed.TotalHours : 0;
        var seriesExpGained = session?.SeriesExpGained ?? 0;

        var strip = cfg.ActiveMode.Id == MatchCountMode.ModeId
            ? $"{Formatting.Elapsed(elapsed)}{Layout.WideDotSeparator}{rate:F1}/h{Layout.WideDotSeparator}+{Formatting.Exp(seriesExpGained)} Series"
            : $"{matches} matches{Layout.WideDotSeparator}{rate:F1}/h{Layout.WideDotSeparator}+{Formatting.Exp(seriesExpGained)} Series";
        Styling.TextCentered(strip, Styling.TextDim);
    }

    private static Display ResolvePhase(AutoPvpSeriesController ctrl, bool inDuty, bool inCombat, bool inQueue, int timeLeft)
    {
        if (ctrl.Phase == AutoPhase.Finishing) return Display.Finishing;
        if (!inDuty) return inQueue ? Display.Queueing : Display.Preparing;
        if (timeLeft > Core.ApsgConstants.CrystallineConflict.IntroBandLowerSec
            && timeLeft < Core.ApsgConstants.CrystallineConflict.IntroBandUpperSec)
            return Display.Portraits;
        if (inCombat) return Display.Fighting;
        return Display.InMatch;
    }

    private static (Vector4 accent, Vector4 accentSoft, string label) Palette(Display phase) => phase switch
    {
        Display.Preparing => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "STARTING"),
        Display.Queueing  => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "IN QUEUE"),
        Display.Portraits => (Styling.AccentAmber,  Styling.AccentAmberSoft,  "PORTRAITS"),
        Display.Fighting  => (Styling.AccentViolet, Styling.AccentVioletSoft, "FIGHTING"),
        Display.Finishing => (Styling.AccentAmber,  Styling.AccentMint,       "DONE"),
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
        var territoryId = Svc.ClientState.TerritoryType;
        if (cachedMapName == null || territoryId != cachedTerritoryId)
        {
            var name = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(territoryId)?.PlaceName.ValueNullable?.Name.ExtractText();
            cachedMapName = string.IsNullOrEmpty(name) ? "the arena" : name;
            cachedTerritoryId = territoryId;
        }

        return cachedMapName;
    }
}
