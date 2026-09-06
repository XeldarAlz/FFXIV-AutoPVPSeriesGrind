using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CombatSettings
{
    public static void Draw(Configuration cfg)
    {
        DrawCombatGroup(cfg);
        SettingsGroup.Footnote(Loc.T(L.Settings.CombatIntroMovement) +
            Loc.T(L.Settings.CombatIntroRotation));

        if (cfg.EnableCombatBrain && cfg.Strategy == PvpStrategy.Custom)
        {
            CustomStrategySettings.Draw(cfg);
        }
    }

    private static void DrawCombatGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupCombat));

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
        SettingsRow.Draw(Loc.T(L.Settings.RotationPlugin),
            Loc.T(L.Settings.RotationPluginHelp),
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
        SettingsRow.Draw(Loc.T(L.Settings.Behavior),
            Loc.T(L.Settings.BehaviorHelp),
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo("##behavior", BehaviorChoices.Options, selected, choiceIndex =>
            {
                BehaviorChoices.Apply(cfg, BehaviorChoices.All[choiceIndex]);
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(Loc.T(BehaviorChoices.All[selected].Detail));
    }

    private static void DrawTargetingRow(Configuration cfg)
    {
        SettingsRow.Draw(Loc.T(L.Settings.SmartTargeting),
            Loc.T(L.Settings.SmartTargetingHelpOn) +
            Loc.T(L.Settings.SmartTargetingHelpManual) +
            Loc.T(L.Settings.SmartTargetingHelpOff),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.BrainPicksTargets, value => cfg.BrainPicksTargets = value, "##cmb_targeting"),
            SettingsRow.ToggleHeight);
    }

    private static void DrawHumanizeRow(Configuration cfg)
    {
        var selected = HumanizeChoices.IndexFor(cfg.Humanize);
        SettingsRow.Draw(Loc.T(L.Settings.ReactionTime),
            Loc.T(L.Settings.ReactionTimeHelp),
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo("##humanize", HumanizeChoices.Options, selected, choiceIndex =>
            {
                cfg.Humanize = HumanizeChoices.All[choiceIndex].Level;
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(Loc.T(HumanizeChoices.All[selected].Detail));
    }

    private static void DrawRecorderRow(Configuration cfg)
    {
        SettingsRow.Draw(Loc.T(L.Settings.RecordMatches),
            Loc.T(L.Settings.RecordMatchesHelp) +
            Loc.T(L.Settings.RecordMatchesHelpSize),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.RecordBrainLogs, value => cfg.RecordBrainLogs = value, "##cmb_record"),
            SettingsRow.ToggleHeight);
    }

    private static class RotationProviderChoices
    {
        public readonly record struct Entry(LocString Name, LocString Detail, RotationProvider Provider);

        public static readonly Entry[] All =
        [
            new(L.Settings.RotationRsr,
                L.Settings.RotationRsrHelp,
                RotationProvider.RotationSolver),
            new(L.Settings.RotationManual,
                L.Settings.RotationManualHelp,
                RotationProvider.External),
        ];

        public static readonly SettingsControls.Choices.Choice[] Options =
            All.Select(entry => new SettingsControls.Choices.Choice(entry.Name, entry.Detail)).ToArray();

        public static int IndexFor(RotationProvider provider)
            => Math.Max(0, Array.FindIndex(All, entry => entry.Provider == provider));
    }

    private static class BehaviorChoices
    {
        public readonly record struct Entry(LocString Name, LocString Detail, bool BrainEnabled, PvpStrategy Strategy);

        public static readonly Entry[] All =
        [
            new(L.Settings.StrategyRush,
                L.Settings.StrategyRushHelp,
                BrainEnabled: false, PvpStrategy.Moderate),
            new(L.Settings.StrategyDefensive,
                L.Settings.StrategyDefensiveHelp,
                BrainEnabled: true, PvpStrategy.Defensive),
            new(L.Settings.StrategyModerate,
                L.Settings.StrategyModerateHelp,
                BrainEnabled: true, PvpStrategy.Moderate),
            new(L.Settings.StrategyAggressive,
                L.Settings.StrategyAggressiveHelp,
                BrainEnabled: true, PvpStrategy.Aggressive),
            new(L.Settings.StrategyCustom,
                L.Settings.StrategyCustomHelp,
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
        public readonly record struct Entry(LocString Name, LocString Detail, HumanizeLevel Level);

        public static readonly Entry[] All =
        [
            new(L.Settings.ReactionOff, L.Settings.ReactionOffHelp, HumanizeLevel.Off),
            new(L.Settings.ReactionLight, L.Settings.ReactionLightHelp, HumanizeLevel.Light),
            new(L.Settings.ReactionRealistic, L.Settings.ReactionRealisticHelp, HumanizeLevel.Realistic),
            new(L.Settings.ReactionHeavy, L.Settings.ReactionHeavyHelp, HumanizeLevel.Heavy),
        ];

        public static readonly SettingsControls.Choices.Choice[] Options =
            All.Select(entry => new SettingsControls.Choices.Choice(entry.Name, entry.Detail)).ToArray();

        public static int IndexFor(HumanizeLevel level)
            => Math.Max(0, Array.FindIndex(All, entry => entry.Level == level));
    }
}
