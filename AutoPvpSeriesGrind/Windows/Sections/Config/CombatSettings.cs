using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CombatSettings
{
    // Brain off is its own choice; the rest map to a PvpStrategy. Name + blurb live together so they
    // cannot drift, and the enum binding means reordering PvpStrategy can't misalign the labels.
    private readonly record struct BehaviorChoice(string Name, string Blurb, bool BrainEnabled, PvpStrategy Strategy);

    private static readonly BehaviorChoice[] BehaviorChoices =
    [
        new("Rush the crystal", "No tactics — just run to the objective and stand on it. Never retreats, never picks targets. Simplest to reason about, but it will feed when outnumbered.", BrainEnabled: false, PvpStrategy.Moderate),
        new("Defensive", "Play smart, cautious: hold the point but back off early (below 50% HP) and never dive. Ranged DPS and healers hold far behind the point.", true, PvpStrategy.Defensive),
        new("Moderate", "Play smart, balanced: hold the point, take short chases, regroup when outnumbered, retreat below 30% HP. A good default.", true, PvpStrategy.Moderate),
        new("Aggressive", "Play smart, aggressive: push the enemy line and chase kills; regroup only when badly outnumbered and retreat only when nearly dead (below 15% HP).", true, PvpStrategy.Aggressive),
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

    public static void Draw(Configuration cfg)
    {
        var mode = cfg.EnableCombatBrain
            ? Math.Max(0, Array.FindIndex(BehaviorChoices, c => c.BrainEnabled && c.Strategy == cfg.Strategy))
            : 0;

        SettingsRow.Draw("Combat behavior", BehaviorChoices[mode].Blurb, () =>
            SettingsControls.DrawCombo("##behavior", BehaviorNames[mode], BehaviorNames, mode, i =>
            {
                var choice = BehaviorChoices[i];
                cfg.EnableCombatBrain = choice.BrainEnabled;
                if (choice.BrainEnabled) cfg.Strategy = choice.Strategy;
                cfg.SaveDebounced();
            }));

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextWrapped("This only controls movement — where to stand and when to back off. RotationSolver still presses your skills (and Guard/Purify); the Limit Break is fired by the required Auto PVP LB plugin, which this plugin auto-configures for your class.");
        ImGui.Spacing();
        ImGui.Spacing();

        if (cfg.EnableCombatBrain && cfg.Strategy == PvpStrategy.Custom)
            DrawCustomStrategy(cfg);

        var humanizeIdx = Math.Max(0, Array.FindIndex(HumanizeChoices, c => c.Level == cfg.Humanize));
        SettingsRow.Draw("Humanize timing", HumanizeChoices[humanizeIdx].Blurb, () =>
            SettingsControls.DrawCombo("##humanize", HumanizeNames[humanizeIdx], HumanizeNames, humanizeIdx, i =>
            {
                cfg.Humanize = HumanizeChoices[i].Level;
                cfg.SaveDebounced();
            }));
    }

    private static void DrawCustomStrategy(Configuration cfg)
    {
        var c = cfg.CustomStrategy;
        ImGui.Spacing();

        Group("Retreat");
        Row(cfg, "Disengage HP", "Back off when pressured or outnumbered and your HP drops below this.",
            () => c.DisengageHpPercent, v => c.DisengageHpPercent = v, 0, 100, "%d%%");
        Row(cfg, "Re-engage HP", "Rejoin the fight once you've healed back above this.",
            () => c.ReengageHpPercent, v => c.ReengageHpPercent = v, 0, 100, "%d%%");
        Row(cfg, "Panic HP", "Always flee below this HP, no matter the situation.",
            () => c.PanicHpPercent, v => c.PanicHpPercent = v, 0, 100, "%d%%");
        Row(cfg, "Pressure count", "How many enemies targeting you counts as 'under pressure'.",
            () => c.FocusRetreatCount, v => c.FocusRetreatCount = v, 1, 8, "%d enemies");

        Group("Aggression");
        Row(cfg, "Push advantage", "Ally-minus-enemy edge around the fight needed before pushing a target. Lower is bolder; 0 pushes on an even fight.",
            () => c.PushAdvantage, v => c.PushAdvantage = v, -2, 4, "%d");
        Row(cfg, "Outnumber margin", "How far behind in numbers before falling back to regroup with the team.",
            () => c.OutnumberMargin, v => c.OutnumberMargin = v, 1, 6, "%d");

        Group("Positioning");
        Row(cfg, "Melee hold", "Yalms a melee holds off the point when not pushing.",
            () => c.MeleeHoldRange, v => c.MeleeHoldRange = v, 0, 15, "%d yd");
        Row(cfg, "Melee reach", "Yalms a melee closes to when pushing a target.",
            () => c.MeleeReach, v => c.MeleeReach = v, 0, 10, "%d yd");
        Row(cfg, "Ranged standoff", "Yalms a backline keeps from the point when holding.",
            () => c.RangedStandoff, v => c.RangedStandoff = v, 0, 30, "%d yd");
        Row(cfg, "Ranged band", "Yalms a backline keeps from its target when pushing.",
            () => c.RangedBand, v => c.RangedBand = v, 0, 30, "%d yd");

        Group("Team & range");
        Row(cfg, "Engage radius", "Yalms around the fight used to weigh the local force balance.",
            () => c.EngageRadius, v => c.EngageRadius = v, 5, 40, "%d yd");
        Row(cfg, "Leash radius", "Yalms from the point the brain will still pick a target (chase limit).",
            () => c.LeashRadius, v => c.LeashRadius = v, 5, 40, "%d yd");
        Row(cfg, "Cohesion radius", "Yalms from the team's center the brain is willing to stray.",
            () => c.CohesionRadius, v => c.CohesionRadius = v, 5, 40, "%d yd");
        Row(cfg, "Kite distance", "Yalms peeled away each step while retreating.",
            () => c.KiteDistance, v => c.KiteDistance = v, 5, 30, "%d yd");

        ImGui.Spacing();
    }

    private static void Group(string label)
    {
        ImGui.Spacing();
        Styling.SectionLabel(label);
    }

    private static void Row(Configuration cfg, string label, string tip, Func<int> get, Action<int> set,
        int min, int max, string fmt)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(label);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(tip);
        ImGui.SameLine(150f * ImGuiHelpers.GlobalScale);
        SettingsControls.DrawIntSlider(cfg, $"##cs_{label}", get, set, min, max, fmt, 175f);
    }
}
