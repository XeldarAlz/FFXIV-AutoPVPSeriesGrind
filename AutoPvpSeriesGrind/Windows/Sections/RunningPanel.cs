using AutoPvpSeriesGrind.Core.Game;
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

// The active-run view: a phase-colored hero card, a Stop button, and a live session strip.
internal static class RunningPanel
{
    // Crystalline Conflict casual matches run 5 minutes; used to scale the live timer bar.
    private const float MatchClockSeconds = 300f;

    private enum Display { Queueing, Portraits, Fighting, InMatch, Finishing }

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        DrawHeaderStrip();
        DrawStatusCard(ctrl);
        ImGui.Spacing();

        if (PrimaryButton.Draw("STOP", Styling.AccentRose))
            ctrl.Stop();

        ImGui.Spacing();
        ImGui.Spacing();
        DrawSessionStrip(ctrl);
        ImGui.Spacing();
        DrawFooter(cfg, ctrl);
    }

    private static void DrawHeaderStrip()
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted("STATUS");
        TopToolbar.DrawIconsInline(Plugin.Instance);
    }

    private static void DrawStatusCard(AutoPvpSeriesController ctrl)
    {
        var inDuty = Svc.Condition[ConditionFlag.BoundByDuty];
        var inCombat = Svc.Condition[ConditionFlag.InCombat];
        var timeLeft = inDuty ? DutyOps.ContentTimeLeft() : 0;

        var phase = ResolvePhase(ctrl, inDuty, inCombat, timeLeft);
        var (accent, accentSoft, label) = Palette(phase);

        var fast = phase is Display.Fighting;
        var border = Styling.PulseColor(accent, accentSoft, fast ? Styling.PulseFast : Styling.PulseMedium);
        var bg = Vector4.Lerp(Styling.CardBg, accent, 0.08f);

        var height = 150f * ImGuiHelpers.GlobalScale;
        using (Card.Begin("##apsg_status", new Vector2(-1, height), bg, border, 2f))
        {
            ImGui.SetWindowFontScale(0.9f);
            using (ImRaii.PushColor(ImGuiCol.Text, accent))
                ImGui.TextUnformatted(label);
            ImGui.SetWindowFontScale(1.0f);

            ImGui.SetWindowFontScale(1.4f);
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
                ImGui.TextUnformatted(ActivityLine(ctrl, phase));
            ImGui.SetWindowFontScale(1.0f);

            ImGui.Spacing();

            if (inDuty && timeLeft > 0 && phase is not Display.Portraits)
            {
                DrawTimerBar(timeLeft, accent);
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                    ImGui.TextUnformatted($"{Formatting.Time(timeLeft)} on the clock");
            }
            else if (phase is Display.Portraits)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                    ImGui.TextUnformatted($"Match starting in {Math.Max(timeLeft, 0)}s…");
            }
            else
            {
                using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
                    ImGui.TextUnformatted(string.IsNullOrWhiteSpace(ctrl.Status) ? "Working…" : ctrl.Status);
            }

            ImGui.Spacing();
            DrawSessionLine(ctrl);
        }
    }

    private static Display ResolvePhase(AutoPvpSeriesController ctrl, bool inDuty, bool inCombat, int timeLeft)
    {
        if (ctrl.Phase == AutoPhase.Finishing) return Display.Finishing;
        if (!inDuty) return Display.Queueing;
        if (timeLeft is > 1 and < 32) return Display.Portraits;
        if (inCombat) return Display.Fighting;
        return Display.InMatch;
    }

    private static (Vector4 accent, Vector4 accentSoft, string label) Palette(Display phase) => phase switch
    {
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
            Display.Queueing  => "Waiting for Casual Match…",
            Display.Portraits => $"Match starting  ·  {CurrentMapName()}",
            Display.Fighting  => $"{jobPrefix}{CurrentMapName()}",
            Display.Finishing => "Wrapping up the session",
            _                 => $"{jobPrefix}{CurrentMapName()}",
        };
    }

    private static void DrawSessionLine(AutoPvpSeriesController ctrl)
    {
        var s = ctrl.SessionSnapshot;
        if (s is null) return;
        var perHour = s.Elapsed.TotalHours > 0 ? s.MatchesCompleted / s.Elapsed.TotalHours : 0;
        var remaining = Plugin.Cfg.ActiveMode.GetRemainingDisplay(s.ToModeContext());
        var remainingSuffix = remaining is null ? "" : $"  ·  {remaining}";
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted($"Session:  {s.MatchesCompleted} matches  ·  {Formatting.Elapsed(s.Elapsed)}  ·  {perHour:F1}/h  ·  {s.Deaths} deaths{remainingSuffix}");
    }

    private static void DrawSessionStrip(AutoPvpSeriesController ctrl)
    {
        var s = ctrl.SessionSnapshot;
        var gap = 7f * ImGuiHelpers.GlobalScale;
        var avail = ImGui.GetContentRegionAvail().X;
        var tileW = (avail - gap * 2f) / 3f;
        var size = new Vector2(tileW, 58f * ImGuiHelpers.GlobalScale);

        var matches = s is null ? "0" : s.MatchesCompleted.ToString();
        var elapsed = s is null ? "0m 00s" : Formatting.Elapsed(s.Elapsed);
        var deaths = s is null ? "0" : s.Deaths.ToString();

        StatTile.Draw(FontAwesomeIcon.Trophy, matches, "Matches", Styling.AccentViolet, size);
        ImGui.SameLine(0, gap);
        StatTile.Draw(FontAwesomeIcon.Stopwatch, elapsed, "Elapsed", Styling.AccentBlue, size);
        ImGui.SameLine(0, gap);
        StatTile.Draw(FontAwesomeIcon.Skull, deaths, "Deaths", Styling.AccentRose, size);
    }

    private static void DrawFooter(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted($"in: {CurrentMapName()}   ·   goal: {cfg.ActiveMode.DisplayName}");
    }

    private static void DrawTimerBar(int timeLeft, Vector4 color)
    {
        var fraction = Math.Clamp(timeLeft / MatchClockSeconds, 0f, 1f);
        var width = ImGui.GetContentRegionAvail().X;
        var height = 16f * ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + new Vector2(width, height);
        var dl = ImGui.GetWindowDrawList();
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(Styling.CardBgSoft), 6f);
        if (fraction > 0)
            dl.AddRectFilled(origin, new Vector2(origin.X + width * fraction, end.Y), ImGui.GetColorU32(color * 0.9f), 6f);
        dl.AddRect(origin, end, ImGui.GetColorU32(Styling.BorderDim), 6f);
        ImGui.Dummy(new Vector2(width, height));
    }

    private static string CurrentMapName()
    {
        var id = Svc.ClientState.TerritoryType;
        var name = Svc.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(id)?.PlaceName.ValueNullable?.Name.ExtractText();
        return string.IsNullOrEmpty(name) ? "the arena" : name;
    }
}
