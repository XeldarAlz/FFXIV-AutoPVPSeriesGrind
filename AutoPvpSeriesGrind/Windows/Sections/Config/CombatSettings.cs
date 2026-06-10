using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CombatSettings
{
    private const float RowLabelOffset = 150f;
    private const float RowSliderWidth = 175f;

    private readonly record struct BehaviorChoice(string Name, string Blurb, bool BrainEnabled, PvpStrategy Strategy);

    private static readonly BehaviorChoice[] BehaviorChoices =
    [
        new("Rush the crystal", "No tactics — just run to the objective and stand on it. Never retreats, never picks targets. Simplest to reason about, but it will feed when outnumbered.", BrainEnabled: false, PvpStrategy.Moderate),
        new("Defensive", "Play smart, cautious: hold the point but never dive, wait for the team before committing, and kite away the moment it's focused. Backs off on any deficit; retreats below ~55% HP. Ranged DPS and healers hold far behind the point.", true, PvpStrategy.Defensive),
        new("Moderate", "Play smart, balanced: hold the point, take short chases when ahead, fall back to the team when outnumbered, and kite out when two enemies focus it. Retreats below ~35% HP. A good default.", true, PvpStrategy.Moderate),
        new("Aggressive", "Play smart, aggressive: push the enemy line and chase kills, but still won't solo a lost fight — falls back only when badly outnumbered and retreats only when nearly dead (below ~18% HP).", true, PvpStrategy.Aggressive),
        new("Custom", "Play smart, hand-tuned: every threshold below is yours to set. Starts from the Moderate baseline.", true, PvpStrategy.Custom),
    ];

    private readonly record struct HumanizeChoice(string Name, string Blurb, HumanizeLevel Level);

    private static readonly HumanizeChoice[] HumanizeChoices =
    [
        new("Off", "React instantly. Movement is frame-perfect.", HumanizeLevel.Off),
        new("Light", "A small reaction delay (~80–220ms) before changing what it's doing.", HumanizeLevel.Light),
        new("Realistic", "A natural reaction delay (~140–380ms). A good default.", HumanizeLevel.Realistic),
        new("Heavy", "A slow, deliberate reaction (~260–650ms) — clearly unhurried.", HumanizeLevel.Heavy),
    ];

    private static readonly string[] BehaviorNames = BehaviorChoices.Select(c => c.Name).ToArray();
    private static readonly string[] HumanizeNames = HumanizeChoices.Select(c => c.Name).ToArray();

    private readonly record struct CustomStrategyRowDescriptor(
        string? GroupHeader,
        string Label,
        string Tooltip,
        int Minimum,
        int Maximum,
        string Format,
        Func<CustomStrategyProfile, int> Getter,
        Action<CustomStrategyProfile, int> Setter)
    {
        public string SliderId { get; } = $"##cs_{Label}";
    }

    private static readonly CustomStrategyRowDescriptor[] customStrategyRows =
    [
        new("Retreat", "Disengage HP", "Back off when pressured or outnumbered and your HP drops below this.",
            0, 100, "%d%%", static custom => custom.DisengageHpPercent, static (custom, value) => custom.DisengageHpPercent = value),
        new(null, "Re-engage HP", "Rejoin the fight once you've healed back above this.",
            0, 100, "%d%%", static custom => custom.ReengageHpPercent, static (custom, value) => custom.ReengageHpPercent = value),
        new(null, "Panic HP", "Always flee below this HP, no matter the situation.",
            0, 100, "%d%%", static custom => custom.PanicHpPercent, static (custom, value) => custom.PanicHpPercent = value),
        new(null, "Pressure count", "How many enemies targeting you counts as 'under pressure' (gates the HP retreat).",
            1, 8, "%d enemies", static custom => custom.FocusRetreatCount, static (custom, value) => custom.FocusRetreatCount = value),
        new("Aggression", "Push advantage", "Ally-minus-enemy edge around you needed before pushing a target. Lower is bolder; 0 pushes on an even fight.",
            -2, 4, "%d", static custom => custom.PushAdvantage, static (custom, value) => custom.PushAdvantage = value),
        new(null, "Outnumber margin", "How far behind in local numbers before falling back to the team. 0 = back off on any deficit.",
            0, 6, "%d", static custom => custom.OutnumberMargin, static (custom, value) => custom.OutnumberMargin = value),
        new("Positioning", "Melee hold", "Yalms a melee holds off the point when not pushing.",
            0, 15, "%d yd", static custom => custom.MeleeHoldRange, static (custom, value) => custom.MeleeHoldRange = value),
        new(null, "Melee reach", "Yalms a melee closes to when pushing a target.",
            0, 10, "%d yd", static custom => custom.MeleeReach, static (custom, value) => custom.MeleeReach = value),
        new(null, "Ranged standoff", "Yalms a backline keeps from the point when holding.",
            0, 30, "%d yd", static custom => custom.RangedStandoff, static (custom, value) => custom.RangedStandoff = value),
        new(null, "Ranged band", "Yalms a backline keeps from its target when pushing.",
            0, 30, "%d yd", static custom => custom.RangedBand, static (custom, value) => custom.RangedBand = value),
        new("Team & range", "Engage radius", "Yalms around the fight used to weigh the local force balance.",
            5, 40, "%d yd", static custom => custom.EngageRadius, static (custom, value) => custom.EngageRadius = value),
        new(null, "Leash radius", "Yalms from the point the brain will still pick a target (chase limit).",
            5, 40, "%d yd", static custom => custom.LeashRadius, static (custom, value) => custom.LeashRadius = value),
        new(null, "Cohesion radius", "Yalms from the team's center the brain is willing to stray.",
            5, 40, "%d yd", static custom => custom.CohesionRadius, static (custom, value) => custom.CohesionRadius = value),
        new(null, "Kite distance", "Yalms peeled away each step while retreating.",
            5, 30, "%d yd", static custom => custom.KiteDistance, static (custom, value) => custom.KiteDistance = value),
        new("Teamplay & focus", "Support radius", "Yalms around you a teammate must be to count as backup. Outside this you're 'alone'.",
            5, 40, "%d yd", static custom => custom.SupportRadius, static (custom, value) => custom.SupportRadius = value),
        new(null, "Threat radius", "Yalms around you used to count nearby enemies when weighing whether you're outnumbered.",
            5, 40, "%d yd", static custom => custom.ThreatRadius, static (custom, value) => custom.ThreatRadius = value),
        new(null, "Focus reposition", "Enemies targeting you that trigger a kite-to-safety before your HP drops.",
            1, 8, "%d enemies", static custom => custom.FocusRepositionCount, static (custom, value) => custom.FocusRepositionCount = value),
        new(null, "Burst sensitivity", "How fast your HP must fall (per second) to count as being bursted and bail early.",
            5, 100, "%d%%/s", static custom => custom.BurstSensitivityPercent, static (custom, value) => custom.BurstSensitivityPercent = value),
        new(null, "Stage standoff", "Yalms to keep from the enemy group while waiting to commit with the team.",
            5, 40, "%d yd", static custom => custom.StageStandoff, static (custom, value) => custom.StageStandoff = value),
        new(null, "Reposition step", "Yalms to peel toward your team when focused.",
            5, 30, "%d yd", static custom => custom.RepositionDistance, static (custom, value) => custom.RepositionDistance = value),
    ];

    public static void Draw(Configuration cfg)
    {
        var behaviorIndex = cfg.EnableCombatBrain
            ? Math.Max(0, FindBehaviorIndex(cfg.Strategy))
            : 0;

        SettingsRow.Draw("Combat behavior", BehaviorChoices[behaviorIndex].Blurb, () =>
            SettingsControls.DrawCombo("##behavior", BehaviorNames[behaviorIndex], BehaviorNames, behaviorIndex, selectedIndex =>
            {
                var choice = BehaviorChoices[selectedIndex];
                cfg.EnableCombatBrain = choice.BrainEnabled;
                if (choice.BrainEnabled)
                {
                    cfg.Strategy = choice.Strategy;
                }

                cfg.SaveDebounced();
            }));

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.TextWrapped("This only controls movement — where to stand and when to back off. RotationSolver still presses your skills (and Guard/Purify); the Limit Break is fired by the required Auto PVP LB plugin, which this plugin auto-configures for your class.");
        }

        ImGui.Spacing();
        ImGui.Spacing();

        if (cfg.EnableCombatBrain)
        {
            DrawTargetPicking(cfg);
        }

        if (cfg.EnableCombatBrain && cfg.Strategy == PvpStrategy.Custom)
        {
            DrawCustomStrategy(cfg);
        }

        var humanizeIndex = Math.Max(0, FindHumanizeIndex(cfg.Humanize));
        SettingsRow.Draw("Humanize timing", HumanizeChoices[humanizeIndex].Blurb, () =>
            SettingsControls.DrawCombo("##humanize", HumanizeNames[humanizeIndex], HumanizeNames, humanizeIndex, selectedIndex =>
            {
                cfg.Humanize = HumanizeChoices[selectedIndex].Level;
                cfg.SaveDebounced();
            }));
    }

    private static void DrawTargetPicking(Configuration cfg)
    {
        SettingsRow.Draw("Smart target picking",
            "On: this plugin decides who to attack — it joins the team's focus target, prefers low-HP and squishy enemies (healers first), and ignores anyone with Guard up. RotationSolver runs in manual mode and presses skills on that target. Off: RotationSolver picks targets itself (always the lowest HP in range).",
            () => SettingsControls.DrawToggle(cfg, () => cfg.BrainPicksTargets, value => cfg.BrainPicksTargets = value));
    }

    private static void DrawCustomStrategy(Configuration cfg)
    {
        var custom = cfg.CustomStrategy;
        ImGui.Spacing();

        for (var rowIndex = 0; rowIndex < customStrategyRows.Length; rowIndex++)
        {
            var descriptor = customStrategyRows[rowIndex];
            if (descriptor.GroupHeader != null)
            {
                Group(descriptor.GroupHeader);
            }

            Row(cfg, custom, descriptor);
        }

        ImGui.Spacing();
    }

    private static void Group(string label)
    {
        ImGui.Spacing();
        Styling.SectionLabel(label);
    }

    private static void Row(Configuration cfg, CustomStrategyProfile custom, CustomStrategyRowDescriptor descriptor)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
        {
            ImGui.TextUnformatted(descriptor.Label);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(descriptor.Tooltip);
        }

        ImGui.SameLine(RowLabelOffset * ImGuiHelpers.GlobalScale);
        SettingsControls.DrawIntSlider(cfg, descriptor.SliderId, () => descriptor.Getter(custom),
            value => descriptor.Setter(custom, value), descriptor.Minimum, descriptor.Maximum, descriptor.Format, RowSliderWidth);
    }

    private static int FindBehaviorIndex(PvpStrategy strategy)
    {
        for (var choiceIndex = 0; choiceIndex < BehaviorChoices.Length; choiceIndex++)
        {
            var choice = BehaviorChoices[choiceIndex];
            if (choice.BrainEnabled && choice.Strategy == strategy)
            {
                return choiceIndex;
            }
        }

        return -1;
    }

    private static int FindHumanizeIndex(HumanizeLevel level)
    {
        for (var choiceIndex = 0; choiceIndex < HumanizeChoices.Length; choiceIndex++)
        {
            if (HumanizeChoices[choiceIndex].Level == level)
            {
                return choiceIndex;
            }
        }

        return -1;
    }
}
