using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Shell;

internal static class MiniPlayer
{
    private const float PadX = 18f;
    private const float ButtonSize = 34f;
    private const float BarWidth = 160f;
    private const float BarHeight = 8f;

    public static bool Draw(Plugin plugin, Vector2 size, float windowRounding)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;
        var info = ReadyState.Resolve(cfg, ctrl);
        var stage = ReadyState.ResolveStage(ctrl);
        var timeLeft = Svc.Condition[ConditionFlag.BoundByDuty] ? DutyOps.ContentTimeLeft() : 0;
        var inMatch = stage is ReadyState.Stage.Fighting or ReadyState.Stage.InMatch && timeLeft > 0;

        Dock.Background(dl, origin, end, windowRounding);

        var padX = PadX * scale;
        var buttonSize = ButtonSize * scale;
        ImGui.SetCursorScreenPos(origin);
        var hit = Hit.Area("##apsg_mini_open", new Vector2(size.X - padX - buttonSize - 8f * scale, size.Y));
        var hover = Motion.Hover(Motion.Key("##apsg_mini_open"), hit.Hovered);
        if (hover > 0.01f)
        {
            Paint.Fill(dl, origin, end, Styling.WithAlpha(Styling.Surface2, 0.35f * hover), windowRounding, ImDrawFlags.RoundCornersBottom);
        }

        var midY = origin.Y + size.Y * 0.5f;
        Paint.Dot(dl, new Vector2(origin.X + padX + 4f * scale, midY), 4f * scale,
            Styling.PulseColor(info.Accent, info.AccentSoft, Styling.PulseMedium));

        var barWidth = BarWidth * scale;
        var barRight = end.X - padX - buttonSize - 16f * scale;
        var barX = barRight - barWidth;
        var barY = midY - BarHeight * scale * 0.5f;
        if (inMatch)
        {
            var remaining = Math.Clamp(timeLeft / (float)Core.ApsgConstants.CrystallineConflict.MatchLengthSec, 0f, 1f);
            Paint.Bar(dl, new Vector2(barX, barY), barWidth, BarHeight * scale, remaining, info.Accent);
        }
        else
        {
            Paint.IndeterminateBar(dl, new Vector2(barX, barY), barWidth, BarHeight * scale, info.Accent);
        }

        var textX = origin.X + padX + 22f * scale;
        var (_, accentSoft, label) = ReadyState.StagePalette(stage);
        var phaseSize = TextDraw.SmallCapsSize(label);
        var lineHeight = ImGui.GetTextLineHeight();
        var gap = 2f * scale;
        var top = midY - (phaseSize.Y + gap + lineHeight) * 0.5f;
        TextDraw.SmallCaps(label, new Vector2(textX, top), accentSoft);

        var main = inMatch ? Loc.T(L.Shell.MatchTimeLeft, Formatting.Time(timeLeft), CurrentMapName()) : ctrl.Status;
        TextDraw.At(TextDraw.Truncate(main, barX - 16f * scale - textX), new Vector2(textX, top + phaseSize.Y + gap), Styling.TextStrong);

        ImGui.SetCursorScreenPos(new Vector2(end.X - padX - buttonSize, midY - buttonSize * 0.5f));
        if (IconButton.Draw(FontAwesomeIcon.Stop, "##apsg_mini_stop", buttonSize, Styling.AccentRose, Loc.T(L.Common.StopRun)))
        {
            ctrl.Stop();
        }

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
        return hit.Clicked;
    }

    private static uint cachedTerritoryId;
    private static string? cachedMapName;

    public static string CurrentMapName()
    {
        var territoryId = Svc.ClientState.TerritoryType;
        if (cachedMapName is null || territoryId != cachedTerritoryId)
        {
            var name = Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.TerritoryType>()?.GetRowOrDefault(territoryId)?.PlaceName.ValueNullable?.Name.ExtractText();
            cachedMapName = string.IsNullOrEmpty(name) ? Loc.T(L.Shell.TheArena) : name;
            cachedTerritoryId = territoryId;
        }

        return cachedMapName;
    }
}
