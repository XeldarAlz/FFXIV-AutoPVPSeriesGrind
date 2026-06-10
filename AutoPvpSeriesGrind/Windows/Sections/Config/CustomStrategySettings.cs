using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CustomStrategySettings
{
    private readonly record struct Row(
        string Label,
        string Help,
        int Minimum,
        int Maximum,
        string Format,
        Func<CustomStrategyProfile, int> Getter,
        Action<CustomStrategyProfile, int> Setter)
    {
        public string SliderId { get; } = $"##cs_{Label}";
    }

    private readonly record struct Group(string Title, Row[] Rows);

    private static readonly Group[] Groups =
    [
        new("Health",
        [
            new("Retreat below", "When enemies are on you, or your side is outnumbered, and your HP drops under this, fall back to safety.",
                0, 100, "%d%%", static p => p.DisengageHpPercent, static (p, v) => p.DisengageHpPercent = v),
            new("Rejoin above", "Once you have healed back above this much HP, return to the fight.",
                0, 100, "%d%%", static p => p.ReengageHpPercent, static (p, v) => p.ReengageHpPercent = v),
            new("Always flee below", "If your HP drops under this, run for safety no matter what else is happening.",
                0, 100, "%d%%", static p => p.PanicHpPercent, static (p, v) => p.PanicHpPercent = v),
            new("Heavy-damage trigger", "If your HP is dropping faster than this each second, treat it as taking heavy damage and back off early.",
                5, 100, "%d%%/s", static p => p.BurstSensitivityPercent, static (p, v) => p.BurstSensitivityPercent = v),
        ]),
        new("Aggression",
        [
            new("Chase when ahead by", "How much stronger your nearby side must be before you chase a kill. Wounded fighters count for less, and each enemy already dead counts as half a fighter. 0 = chase even fights; below 0 = chase even when slightly outnumbered.",
                -2, 4, "%d", static p => p.PushAdvantage, static (p, v) => p.PushAdvantage = v),
            new("Fall back when behind by", "How much stronger the nearby enemies must be before you retreat to your team. Wounded fighters count for less. 0 = fall back the moment they outnumber you.",
                0, 6, "%d", static p => p.OutnumberMargin, static (p, v) => p.OutnumberMargin = v),
        ]),
        new("Where to stand",
        [
            new("Melee: hold near crystal", "As melee or tank with no one to chase, how far from the crystal to stand.",
                0, 15, "%d yd", static p => p.MeleeHoldRange, static (p, v) => p.MeleeHoldRange = v),
            new("Melee: chase up to", "As melee or tank chasing a target, how close to get before attacking.",
                0, 10, "%d yd", static p => p.MeleeReach, static (p, v) => p.MeleeReach = v),
            new("Ranged: hold behind crystal", "As ranged or healer holding position, how far behind the crystal to stand.",
                0, 30, "%d yd", static p => p.RangedStandoff, static (p, v) => p.RangedStandoff = v),
            new("Ranged: attack from", "As ranged or healer, how far from your target to stand while attacking.",
                0, 30, "%d yd", static p => p.RangedBand, static (p, v) => p.RangedBand = v),
            new("Keep back while waiting", "While waiting for teammates to arrive, keep at least this far from the enemy group.",
                5, 40, "%d yd", static p => p.StageStandoff, static (p, v) => p.StageStandoff = v),
        ]),
        new("When enemies focus you",
        [
            new("In danger at", "How many enemies aiming at you before you count as in danger. Works with Retreat below: the more attackers, the sooner you fall back.",
                1, 8, "%d attackers", static p => p.FocusRetreatCount, static (p, v) => p.FocusRetreatCount = v),
            new("Sidestep at", "When at least this many enemies are aiming at you, sidestep to safety even at full HP.",
                1, 8, "%d attackers", static p => p.FocusRepositionCount, static (p, v) => p.FocusRepositionCount = v),
            new("Sidestep distance", "When enemies focus you, how far to move in one sidestep, toward whatever spot is safest and closest to your team.",
                5, 30, "%d yd", static p => p.RepositionDistance, static (p, v) => p.RepositionDistance = v),
            new("Retreat step", "How far each step of a retreat takes you. The direction is chosen to slip past enemies, stay near your team, and break their line of sight.",
                5, 30, "%d yd", static p => p.KiteDistance, static (p, v) => p.KiteDistance = v),
        ]),
        new("Distances & limits",
        [
            new("Backup nearby range", "A teammate within this distance counts as backup. With nobody inside it, you act as if you are alone.",
                5, 40, "%d yd", static p => p.SupportRadius, static (p, v) => p.SupportRadius = v),
            new("Enemy-near range", "Enemies within this distance of you count toward being outnumbered.",
                5, 40, "%d yd", static p => p.ThreatRadius, static (p, v) => p.ThreatRadius = v),
            new("Fight-zone size", "The area around the crystal used to judge who is winning the fight. When no enemy is inside it, the point counts as free and you stand on it to push.",
                5, 40, "%d yd", static p => p.EngageRadius, static (p, v) => p.EngageRadius = v),
            new("Max chase from crystal", "Never chase a target further than this from the crystal. Enemies past it are ignored.",
                5, 40, "%d yd", static p => p.LeashRadius, static (p, v) => p.LeashRadius = v),
            new("Max distance from team", "Never wander further than this from the middle of your team.",
                5, 40, "%d yd", static p => p.CohesionRadius, static (p, v) => p.CohesionRadius = v),
        ]),
    ];

    public static void Draw(Configuration cfg)
    {
        var custom = cfg.CustomStrategy;
        foreach (var group in Groups)
        {
            DrawGroup(cfg, custom, group);
        }

        DrawResetRow(cfg);
    }

    private static void DrawGroup(Configuration cfg, CustomStrategyProfile custom, Group group)
    {
        using var card = SettingsGroup.Begin(group.Title);
        foreach (var row in group.Rows)
        {
            DrawRow(cfg, custom, row);
        }
    }

    private static void DrawRow(Configuration cfg, CustomStrategyProfile custom, Row row)
    {
        SettingsRow.Draw(row.Label, row.Help, SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, row.SliderId,
                () => row.Getter(custom), value => row.Setter(custom, value),
                row.Minimum, row.Maximum, row.Format));
    }

    private const float ResetButtonWidth = 170f;

    private static void DrawResetRow(Configuration cfg)
    {
        var armed = ImGui.GetIO().KeyCtrl;
        var width = ResetButtonWidth * ImGuiHelpers.GlobalScale;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, ImGui.GetContentRegionAvail().X - width));

        using var colors = ImRaii.PushColor(ImGuiCol.Button, Styling.CardBgSoft)
            .Push(ImGuiCol.ButtonHovered, Styling.WithAlpha(Styling.AccentRose, 0.30f))
            .Push(ImGuiCol.ButtonActive, Styling.WithAlpha(Styling.AccentRose, 0.45f))
            .Push(ImGuiCol.Border, Styling.BorderDim)
            .Push(ImGuiCol.Text, armed ? Styling.AccentRose : Styling.TextDim);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, Styling.FrameRounding);
        using var border = ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1f);

        if (ImGui.Button("Reset to defaults", new Vector2(width, ImGui.GetFrameHeight())) && armed)
        {
            cfg.CustomStrategy = new CustomStrategyProfile();
            cfg.Save();
        }

        if (ImGui.IsItemHovered())
        {
            SettingsRow.HelpTooltip("Sets every value above back to the Moderate baseline. Hold Ctrl and click to confirm.");
        }
    }
}
