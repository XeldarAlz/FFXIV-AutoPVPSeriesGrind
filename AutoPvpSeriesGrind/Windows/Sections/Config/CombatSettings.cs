using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CombatSettings
{
    public static void Draw(Configuration cfg)
    {
        DrawCombatGroup(cfg);
        SettingsGroup.Footnote("Behavior controls movement only: where to stand and when to back off. " +
            "Your rotation plugin presses the skills; the required Auto PVP LB plugin fires the Limit Break, auto-configured for your class.");

        if (cfg.EnableCombatBrain && cfg.Strategy == PvpStrategy.Custom)
        {
            CustomStrategySettings.Draw(cfg);
        }
    }

    private static void DrawCombatGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin("Combat");

        DrawRotationProviderRow(cfg);
        DrawBehaviorRow(cfg);

        if (cfg.EnableCombatBrain)
        {
            DrawTargetingRow(cfg);
        }

        DrawHumanizeRow(cfg);
        DrawRecorderRow(cfg);
    }

    private static void DrawRotationProviderRow(Configuration cfg)
    {
        var selected = RotationProviderChoices.IndexFor(cfg.RotationProvider);
        SettingsRow.Draw("Rotation plugin",
            "Which plugin presses your combat skills during matches.",
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo("##rotprovider", RotationProviderChoices.Options, selected, choiceIndex =>
            {
                cfg.RotationProvider = RotationProviderChoices.All[choiceIndex].Provider;
                cfg.SaveDebounced();
            }));
    }

    private static void DrawBehaviorRow(Configuration cfg)
    {
        var selected = BehaviorChoices.IndexFor(cfg);
        SettingsRow.Draw("Behavior",
            "How the bot positions itself and picks its fights.",
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo("##behavior", BehaviorChoices.Options, selected, choiceIndex =>
            {
                BehaviorChoices.Apply(cfg, BehaviorChoices.All[choiceIndex]);
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(BehaviorChoices.All[selected].Detail);
    }

    private static void DrawTargetingRow(Configuration cfg)
    {
        SettingsRow.Draw("Smart targeting",
            "On: this plugin decides who to attack. It joins the team's focus target, prefers low-HP and squishy enemies (healers first), and skips anyone with Guard up. " +
            "RotationSolver runs in manual mode and presses skills on that target; another rotation plugin must attack your current target. " +
            "Off: the rotation plugin picks targets itself (RotationSolver uses lowest HP in range).",
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.BrainPicksTargets, value => cfg.BrainPicksTargets = value),
            SettingsRow.ToggleHeight);
    }

    private static void DrawHumanizeRow(Configuration cfg)
    {
        var selected = HumanizeChoices.IndexFor(cfg.Humanize);
        SettingsRow.Draw("Reaction time",
            "Adds a human reaction delay before the bot changes what it's doing.",
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo("##humanize", HumanizeChoices.Options, selected, choiceIndex =>
            {
                cfg.Humanize = HumanizeChoices.All[choiceIndex].Level;
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(HumanizeChoices.All[selected].Detail);
    }

    private static void DrawRecorderRow(Configuration cfg)
    {
        SettingsRow.Draw("Record matches",
            "Writes every brain decision (positions, HP, posture, reason) to a per-match log file in the plugin folder, " +
            "for reviewing and tuning how it played. Roughly 1 MB per match; only the last 30 matches are kept.",
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.RecordBrainLogs, value => cfg.RecordBrainLogs = value),
            SettingsRow.ToggleHeight);
    }

    private static class RotationProviderChoices
    {
        public readonly record struct Entry(string Name, string Detail, RotationProvider Provider);

        public static readonly Entry[] All =
        [
            new("RotationSolver Reborn",
                "Auto-installed and driven by this plugin; skills, Guard, and Purify are handled for you. The recommended default.",
                RotationProvider.RotationSolver),
            new("Other / manual",
                "Bring your own rotation plugin (e.g. Wrath Combo). It must press skills, Guard, and Purify itself; RotationSolver is no longer required.",
                RotationProvider.External),
        ];

        public static readonly SettingsControls.Choices.Choice[] Options =
            All.Select(entry => new SettingsControls.Choices.Choice(entry.Name, entry.Detail)).ToArray();

        public static int IndexFor(RotationProvider provider)
            => Math.Max(0, Array.FindIndex(All, entry => entry.Provider == provider));
    }

    private static class BehaviorChoices
    {
        public readonly record struct Entry(string Name, string Detail, bool BrainEnabled, PvpStrategy Strategy);

        public static readonly Entry[] All =
        [
            new("Rush the crystal",
                "No tactics: runs to the objective and stands on it. Never retreats; will feed when outnumbered.",
                BrainEnabled: false, PvpStrategy.Moderate),
            new("Defensive",
                "Holds the point without diving, kites when focused, retreats below ~55% HP. Ranged and healers stay far back.",
                BrainEnabled: true, PvpStrategy.Defensive),
            new("Moderate",
                "Balanced: short chases when ahead, falls back when outnumbered, retreats below ~35% HP. A good default.",
                BrainEnabled: true, PvpStrategy.Moderate),
            new("Aggressive",
                "Pushes the enemy line and chases kills; retreats only when nearly dead (~18% HP).",
                BrainEnabled: true, PvpStrategy.Aggressive),
            new("Custom",
                "Hand-tuned: every threshold below is yours to set. Starts from the Moderate baseline.",
                BrainEnabled: true, PvpStrategy.Custom),
        ];

        public static readonly SettingsControls.Choices.Choice[] Options =
            All.Select(entry => new SettingsControls.Choices.Choice(entry.Name, entry.Detail)).ToArray();

        public static int IndexFor(Configuration cfg)
        {
            if (!cfg.EnableCombatBrain)
            {
                return 0;
            }

            return Math.Max(0, Array.FindIndex(All, entry => entry.BrainEnabled && entry.Strategy == cfg.Strategy));
        }

        public static void Apply(Configuration cfg, Entry entry)
        {
            cfg.EnableCombatBrain = entry.BrainEnabled;
            if (entry.BrainEnabled)
            {
                cfg.Strategy = entry.Strategy;
            }
        }
    }

    private static class HumanizeChoices
    {
        public readonly record struct Entry(string Name, string Detail, HumanizeLevel Level);

        public static readonly Entry[] All =
        [
            new("Off", "Reacts instantly, with frame-perfect movement.", HumanizeLevel.Off),
            new("Light", "~80–220 ms reaction delay.", HumanizeLevel.Light),
            new("Realistic", "~140–380 ms reaction delay. A good default.", HumanizeLevel.Realistic),
            new("Heavy", "~260–650 ms reaction delay, clearly unhurried.", HumanizeLevel.Heavy),
        ];

        public static readonly SettingsControls.Choices.Choice[] Options =
            All.Select(entry => new SettingsControls.Choices.Choice(entry.Name, entry.Detail)).ToArray();

        public static int IndexFor(HumanizeLevel level)
            => Math.Max(0, Array.FindIndex(All, entry => entry.Level == level));
    }
}
