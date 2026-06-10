using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Windows.Components;

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
        new("Retreat",
        [
            new("Disengage HP", "If enemies are on you (or your side is outnumbered) and your HP falls below this, run back to safety.",
                0, 100, "%d%%", static p => p.DisengageHpPercent, static (p, v) => p.DisengageHpPercent = v),
            new("Re-engage HP", "Once you have recovered above this much HP, go back into the fight.",
                0, 100, "%d%%", static p => p.ReengageHpPercent, static (p, v) => p.ReengageHpPercent = v),
            new("Panic HP", "Below this HP, always run away, no exceptions.",
                0, 100, "%d%%", static p => p.PanicHpPercent, static (p, v) => p.PanicHpPercent = v),
            new("Pressure count", "How many enemies must be targeting you before you count as 'in danger'. Works together with Disengage HP.",
                1, 8, "%d enemies", static p => p.FocusRetreatCount, static (p, v) => p.FocusRetreatCount = v),
        ]),
        new("Aggression",
        [
            new("Push advantage", "How much stronger your side must be nearby before you chase a kill. Wounded fighters count for less, and every extra enemy death counts as half a fighter. 0 = chase even fights; negative = chase even when weaker.",
                -2, 4, "%d", static p => p.PushAdvantage, static (p, v) => p.PushAdvantage = v),
            new("Outnumber margin", "How much stronger the enemies near you must be before you fall back to your team. Wounded fighters count for less. 0 = fall back as soon as they have any edge.",
                0, 6, "%d", static p => p.OutnumberMargin, static (p, v) => p.OutnumberMargin = v),
        ]),
        new("Positioning",
        [
            new("Melee hold", "How far from the crystal to stand as melee or tank when not chasing anyone.",
                0, 15, "%d yd", static p => p.MeleeHoldRange, static (p, v) => p.MeleeHoldRange = v),
            new("Melee reach", "How close to get to a target when chasing it as melee or tank.",
                0, 10, "%d yd", static p => p.MeleeReach, static (p, v) => p.MeleeReach = v),
            new("Ranged standoff", "How far behind the crystal to stand as ranged or healer when holding.",
                0, 30, "%d yd", static p => p.RangedStandoff, static (p, v) => p.RangedStandoff = v),
            new("Ranged band", "How far to stay from your target when attacking as ranged or healer.",
                0, 30, "%d yd", static p => p.RangedBand, static (p, v) => p.RangedBand = v),
        ]),
        new("Team & range",
        [
            new("Engage radius", "Size of the area around the fight used to count who is winning it. When no enemy is within it of the crystal, the point counts as free and you stand on it to push.",
                5, 40, "%d yd", static p => p.EngageRadius, static (p, v) => p.EngageRadius = v),
            new("Leash radius", "Never chase a target further than this from the crystal. Enemies beyond it are ignored.",
                5, 40, "%d yd", static p => p.LeashRadius, static (p, v) => p.LeashRadius = v),
            new("Cohesion radius", "Never wander further than this from the middle of your team.",
                5, 40, "%d yd", static p => p.CohesionRadius, static (p, v) => p.CohesionRadius = v),
            new("Kite distance", "How far each retreat step takes you. The direction is picked to slip past enemies, stay near your team, and break their line of sight.",
                5, 30, "%d yd", static p => p.KiteDistance, static (p, v) => p.KiteDistance = v),
        ]),
        new("Teamplay & focus",
        [
            new("Support radius", "A teammate within this distance counts as backup. With no one inside it, you are treated as alone.",
                5, 40, "%d yd", static p => p.SupportRadius, static (p, v) => p.SupportRadius = v),
            new("Threat radius", "Enemies within this distance of you count toward being outnumbered.",
                5, 40, "%d yd", static p => p.ThreatRadius, static (p, v) => p.ThreatRadius = v),
            new("Focus reposition", "When this many enemies target you, sidestep to safety even at full HP.",
                1, 8, "%d enemies", static p => p.FocusRepositionCount, static (p, v) => p.FocusRepositionCount = v),
            new("Burst sensitivity", "How fast your HP must be dropping (percent per second) to count as being bursted and back off early.",
                5, 100, "%d%%/s", static p => p.BurstSensitivityPercent, static (p, v) => p.BurstSensitivityPercent = v),
            new("Stage standoff", "While waiting for your team to arrive, keep at least this far from the enemy group.",
                5, 40, "%d yd", static p => p.StageStandoff, static (p, v) => p.StageStandoff = v),
            new("Reposition step", "When enemies target you, how far to move in one step, in whichever direction is safest and closest to your team.",
                5, 30, "%d yd", static p => p.RepositionDistance, static (p, v) => p.RepositionDistance = v),
        ]),
    ];

    public static void Draw(Configuration cfg)
    {
        var custom = cfg.CustomStrategy;
        foreach (var group in Groups)
        {
            DrawGroup(cfg, custom, group);
        }
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
}
