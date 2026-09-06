using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CustomStrategySettings
{
    private readonly record struct Row(
        LocString Label,
        LocString Help,
        int Minimum,
        int Maximum,
        LocString Format,
        Func<CustomStrategyProfile, int> Getter,
        Action<CustomStrategyProfile, int> Setter)
    {
        public string SliderId { get; } = $"##cs_{Label.Key}";
    }

    private readonly record struct Group(LocString Title, Row[] Rows);

    private static readonly Group[] Groups =
    [
        new(L.Settings.GroupHealth,
        [
            new(L.Settings.RetreatBelow, L.Settings.RetreatBelowHelp,
                0, 100, L.Settings.FormatPercent, static p => p.DisengageHpPercent, static (p, v) => p.DisengageHpPercent = v),
            new(L.Settings.RejoinAbove, L.Settings.RejoinAboveHelp,
                0, 100, L.Settings.FormatPercent, static p => p.ReengageHpPercent, static (p, v) => p.ReengageHpPercent = v),
            new(L.Settings.AlwaysFleeBelow, L.Settings.AlwaysFleeBelowHelp,
                0, 100, L.Settings.FormatPercent, static p => p.PanicHpPercent, static (p, v) => p.PanicHpPercent = v),
            new(L.Settings.HeavyDamage, L.Settings.HeavyDamageHelp,
                5, 100, L.Settings.FormatPercentPerSecond, static p => p.BurstSensitivityPercent, static (p, v) => p.BurstSensitivityPercent = v),
        ]),
        new(L.Settings.GroupAggression,
        [
            new(L.Settings.ChaseAhead, L.Settings.ChaseAheadHelp,
                -2, 4, L.Settings.FormatCount, static p => p.PushAdvantage, static (p, v) => p.PushAdvantage = v),
            new(L.Settings.FallBackBehind, L.Settings.FallBackBehindHelp,
                0, 6, L.Settings.FormatCount, static p => p.OutnumberMargin, static (p, v) => p.OutnumberMargin = v),
        ]),
        new(L.Settings.GroupPositioning,
        [
            new(L.Settings.MeleeHold, L.Settings.MeleeHoldHelp,
                0, 15, L.Settings.FormatYards, static p => p.MeleeHoldRange, static (p, v) => p.MeleeHoldRange = v),
            new(L.Settings.MeleeChase, L.Settings.MeleeChaseHelp,
                0, 10, L.Settings.FormatYards, static p => p.MeleeReach, static (p, v) => p.MeleeReach = v),
            new(L.Settings.RangedHold, L.Settings.RangedHoldHelp,
                0, 30, L.Settings.FormatYards, static p => p.RangedStandoff, static (p, v) => p.RangedStandoff = v),
            new(L.Settings.RangedAttack, L.Settings.RangedAttackHelp,
                0, 30, L.Settings.FormatYards, static p => p.RangedBand, static (p, v) => p.RangedBand = v),
            new(L.Settings.KeepBack, L.Settings.KeepBackHelp,
                5, 40, L.Settings.FormatYards, static p => p.StageStandoff, static (p, v) => p.StageStandoff = v),
        ]),
        new(L.Settings.GroupFocus,
        [
            new(L.Settings.InDangerAt, L.Settings.InDangerAtHelp,
                1, 8, L.Settings.FormatAttackers, static p => p.FocusRetreatCount, static (p, v) => p.FocusRetreatCount = v),
            new(L.Settings.SidestepAt, L.Settings.SidestepAtHelp,
                1, 8, L.Settings.FormatAttackers, static p => p.FocusRepositionCount, static (p, v) => p.FocusRepositionCount = v),
            new(L.Settings.SidestepDistance, L.Settings.SidestepDistanceHelp,
                5, 30, L.Settings.FormatYards, static p => p.RepositionDistance, static (p, v) => p.RepositionDistance = v),
            new(L.Settings.RetreatStep, L.Settings.RetreatStepHelp,
                5, 30, L.Settings.FormatYards, static p => p.KiteDistance, static (p, v) => p.KiteDistance = v),
        ]),
        new(L.Settings.GroupLimits,
        [
            new(L.Settings.BackupRange, L.Settings.BackupRangeHelp,
                5, 40, L.Settings.FormatYards, static p => p.SupportRadius, static (p, v) => p.SupportRadius = v),
            new(L.Settings.EnemyNearRange, L.Settings.EnemyNearRangeHelp,
                5, 40, L.Settings.FormatYards, static p => p.ThreatRadius, static (p, v) => p.ThreatRadius = v),
            new(L.Settings.FightZone, L.Settings.FightZoneHelp,
                5, 40, L.Settings.FormatYards, static p => p.EngageRadius, static (p, v) => p.EngageRadius = v),
            new(L.Settings.MaxChase, L.Settings.MaxChaseHelp,
                5, 40, L.Settings.FormatYards, static p => p.LeashRadius, static (p, v) => p.LeashRadius = v),
            new(L.Settings.MaxFromTeam, L.Settings.MaxFromTeamHelp,
                5, 40, L.Settings.FormatYards, static p => p.CohesionRadius, static (p, v) => p.CohesionRadius = v),
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
        using var card = SettingsGroup.Begin(Loc.T(group.Title));
        foreach (var row in group.Rows)
        {
            DrawRow(cfg, custom, row);
        }
    }

    private static void DrawRow(Configuration cfg, CustomStrategyProfile custom, Row row)
    {
        SettingsRow.Draw(Loc.T(row.Label), Loc.T(row.Help), SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, row.SliderId,
                () => row.Getter(custom), value => row.Setter(custom, value),
                row.Minimum, row.Maximum, Loc.T(row.Format)));
    }

    private static void DrawResetRow(Configuration cfg)
    {
        var resetLabel = Loc.T(L.Settings.ResetDefaults);
        var armed = ImGui.GetIO().KeyCtrl;
        var width = PillButton.Width(resetLabel, FontAwesomeIcon.Undo);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, ImGui.GetContentRegionAvail().X - width));

        var emphasis = armed ? PillButton.Emphasis.Tinted : PillButton.Emphasis.Ghost;
        if (PillButton.Draw("##cs_reset", resetLabel, Styling.AccentRose, emphasis, FontAwesomeIcon.Undo,
                tooltip: Loc.T(L.Settings.ResetDefaultsHelp))
            && armed)
        {
            cfg.CustomStrategy = new CustomStrategyProfile();
            cfg.Save();
        }
    }
}
