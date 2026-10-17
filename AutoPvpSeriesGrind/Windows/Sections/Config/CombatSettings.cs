using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CombatSettings
{
    public static void Draw(Configuration cfg, bool running = false)
    {
        if (running)
        {
            ChoiceCards.Description(Loc.T(L.Setup.CombatLocked), Styling.AccentAmberSoft);
            using (SettingsGroup.Begin(Loc.T(L.Settings.ManualMovement))) DrawSteeringRow(cfg);
        }
        using var disabled = Dalamud.Interface.Utility.Raii.ImRaii.Disabled(running);
        ModePicker.Draw(cfg, !running);
        Styling.VSpace(18f);
        CombatSetup.Draw(cfg, showSettingsLink: false);
        Styling.VSpace(12f);
        DrawCombatGroup(cfg, running);
        if (cfg.RotationProvider == RotationProvider.Internal) DrawRotationGroup(cfg);
    }

    private static void DrawCombatGroup(Configuration cfg, bool running)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupCombat));

        DrawRotationProviderRow(cfg);
        if (!running) DrawSteeringRow(cfg);
        DrawTargetingRow(cfg);
        if (!cfg.ManualMovement) DrawHumanizeRow(cfg);
        if (!cfg.MatchType.IsLargeScale() && !cfg.ManualMovement)
        {
            SettingsRow.Draw(Loc.T(L.Settings.StrategyRush), Loc.T(L.Settings.StrategyRushHelp), SettingsControls.ToggleWidth,
                () => SettingsControls.DrawToggle(cfg, () => !cfg.EnableCombatBrain,
                    value => cfg.EnableCombatBrain = !value, "##legacy_movement"), SettingsRow.ToggleHeight);
        }

        DrawRecorderRow(cfg);
        SettingsRow.Draw(Loc.T(L.Settings.AvoidChiten), Loc.T(L.Settings.AvoidChitenHelp), SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.AvoidChiten, value => cfg.AvoidChiten = value, "##avoid_chiten"),
            SettingsRow.ToggleHeight);
    }

    private static void DrawSteeringRow(Configuration cfg)
    {
        SettingsRow.Draw(Loc.T(L.Settings.ManualMovement),
            Loc.T(L.Settings.ManualMovementHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.ManualMovement, value => cfg.ManualMovement = value, "##cmb_steer"),
            SettingsRow.ToggleHeight);
    }

    private static void DrawRotationGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupRotation));

        SettingsRow.Draw(Loc.T(L.Settings.RotationGuardHp),
            Loc.T(L.Settings.RotationGuardHpHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##rot_guard",
                () => cfg.RotationGuardHpPercent, value => cfg.RotationGuardHpPercent = value, 5, 50, Loc.T(L.Settings.FormatPercent)));

        SettingsRow.Draw(Loc.T(L.Settings.RotationGuardOnBurst),
            Loc.T(L.Settings.RotationGuardOnBurstHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.RotationGuardOnBurst, value => cfg.RotationGuardOnBurst = value, "##rot_guardburst"),
            SettingsRow.ToggleHeight);

        if (cfg.RotationGuardOnBurst)
        {
            SettingsRow.Draw(Loc.T(L.Settings.RotationGuardOnBurstHp),
                Loc.T(L.Settings.RotationGuardOnBurstHpHelp),
                SettingsControls.RowSliderWidth,
                () => SettingsControls.DrawIntSlider(cfg, "##rot_guardbursthp",
                    () => cfg.RotationGuardOnBurstHpPercent, value => cfg.RotationGuardOnBurstHpPercent = value, 20, 80, Loc.T(L.Settings.FormatPercent)));
        }

        SettingsRow.Draw(Loc.T(L.Settings.RotationRecuperate),
            Loc.T(L.Settings.RotationRecuperateHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##rot_recuperate",
                () => cfg.RotationRecuperateMissingHp, value => cfg.RotationRecuperateMissingHp = value, 5000, 30000, Loc.T(L.Settings.FormatHp)));

        SettingsRow.Draw(Loc.T(L.Settings.RotationElixir),
            Loc.T(L.Settings.RotationElixirHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##rot_elixir",
                () => cfg.RotationElixirPercent, value => cfg.RotationElixirPercent = value, 10, 60, Loc.T(L.Settings.FormatPercent)));

        SettingsRow.Draw(Loc.T(L.Settings.RotationAllySupport),
            Loc.T(L.Settings.RotationAllySupportHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##rot_ally",
                () => cfg.RotationAllySupportHpPercent, value => cfg.RotationAllySupportHpPercent = value, 20, 90, Loc.T(L.Settings.FormatPercent)));

        SettingsRow.Draw(Loc.T(L.Settings.RotationPurify),
            Loc.T(L.Settings.RotationPurifyHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.RotationPurify, value => cfg.RotationPurify = value, "##rot_purify"),
            SettingsRow.ToggleHeight);
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

    private static void DrawTargetingRow(Configuration cfg)
    {
        var selected = TargetingChoices.IndexFor(cfg);
        SettingsRow.Draw(Loc.T(L.Settings.Targeting),
            Loc.T(L.Settings.TargetingHelp),
            SettingsControls.RowComboWidth,
            () => SettingsControls.Choices.DrawCombo("##targeting", TargetingChoices.Options, selected, choiceIndex =>
            {
                TargetingChoices.Apply(cfg, TargetingChoices.All[choiceIndex]);
                cfg.SaveDebounced();
            }));

        SettingsRow.Caption(Loc.T(TargetingChoices.All[selected].Detail));
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
            new(L.Settings.RotationBuiltIn,
                L.Settings.RotationBuiltInHelp,
                RotationProvider.Internal),
            new(L.Settings.RotationManual,
                L.Settings.RotationManualHelp,
                RotationProvider.Manual),
        ];

        public static readonly SettingsControls.Choices.Choice[] Options =
            All.Select(entry => new SettingsControls.Choices.Choice(entry.Name, entry.Detail)).ToArray();

        public static int IndexFor(RotationProvider provider)
            => Math.Max(0, Array.FindIndex(All, entry => entry.Provider == provider));
    }

    private static class TargetingChoices
    {
        public readonly record struct Entry(LocString Name, LocString Detail, bool BrainPicks, TargetingMode Mode);

        public static readonly Entry[] All =
        [
            new(L.Settings.TargetingSmart, L.Settings.TargetingSmartHelp, BrainPicks: true, TargetingMode.Smart),
            new(L.Settings.TargetingLowestHp, L.Settings.TargetingLowestHpHelp, BrainPicks: true, TargetingMode.LowestHp),
            new(L.Settings.TargetingCurrent, L.Settings.TargetingCurrentHelp, BrainPicks: false, TargetingMode.Smart),
        ];

        public static readonly SettingsControls.Choices.Choice[] Options =
            All.Select(entry => new SettingsControls.Choices.Choice(entry.Name, entry.Detail)).ToArray();

        public static int IndexFor(Configuration cfg)
        {
            if (!cfg.BrainPicksTargets)
            {
                return 2;
            }
            return cfg.Targeting == TargetingMode.LowestHp ? 1 : 0;
        }

        public static void Apply(Configuration cfg, Entry entry)
        {
            cfg.BrainPicksTargets = entry.BrainPicks;
            cfg.Targeting = entry.Mode;
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
