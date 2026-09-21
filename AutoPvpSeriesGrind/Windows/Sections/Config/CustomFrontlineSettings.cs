using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class CustomFrontlineSettings
{
    private readonly record struct Row(
        LocString Label,
        LocString Help,
        int Minimum,
        int Maximum,
        LocString Format,
        Func<CustomFrontlineProfile, int> Getter,
        Action<CustomFrontlineProfile, int> Setter)
    {
        public string SliderId { get; } = $"##cf_{Label.Key}";
    }

    private readonly record struct Group(LocString Title, Row[] Rows);

    private static readonly Group[] Groups =
    [
        new(L.Settings.GroupHealth,
        [
            new(L.Settings.FrontlineHurtBelow, L.Settings.FrontlineHurtBelowHelp,
                0, 100, L.Settings.FormatPercent, static p => p.HurtHpPercent, static (p, v) => p.HurtHpPercent = v),
            new(L.Settings.FrontlineFocusedHurtBelow, L.Settings.FrontlineFocusedHurtBelowHelp,
                0, 100, L.Settings.FormatPercent, static p => p.FocusedHurtHpPercent, static (p, v) => p.FocusedHurtHpPercent = v),
            new(L.Settings.FrontlineRecoverAbove, L.Settings.FrontlineRecoverAboveHelp,
                0, 100, L.Settings.FormatPercent, static p => p.RecoveredHpPercent, static (p, v) => p.RecoveredHpPercent = v),
            new(L.Settings.HeavyDamage, L.Settings.HeavyDamageHelp,
                5, 100, L.Settings.FormatPercentPerSecond, static p => p.BurstSensitivityPercent, static (p, v) => p.BurstSensitivityPercent = v),
        ]),
        new(L.Settings.FrontlineGroupPosition,
        [
            new(L.Settings.FrontlineFrontOffset, L.Settings.FrontlineFrontOffsetHelp,
                0, 15, L.Settings.FormatYards, static p => p.FrontOffset, static (p, v) => p.FrontOffset = v),
            new(L.Settings.FrontlineBackOffset, L.Settings.FrontlineBackOffsetHelp,
                0, 15, L.Settings.FormatYards, static p => p.BackOffset, static (p, v) => p.BackOffset = v),
            new(L.Settings.FrontlineRetreatOffset, L.Settings.FrontlineRetreatOffsetHelp,
                0, 30, L.Settings.FormatYards, static p => p.RetreatOffset, static (p, v) => p.RetreatOffset = v),
        ]),
        new(L.Settings.FrontlineGroupCohesion,
        [
            new(L.Settings.FrontlineRegroupDistance, L.Settings.FrontlineRegroupDistanceHelp,
                10, 50, L.Settings.FormatYards, static p => p.RegroupDistance, static (p, v) => p.RegroupDistance = v),
            new(L.Settings.FrontlineEngageRange, L.Settings.FrontlineEngageRangeHelp,
                5, 30, L.Settings.FormatYards, static p => p.EngageRange, static (p, v) => p.EngageRange = v),
            new(L.Settings.InDangerAt, L.Settings.InDangerAtHelp,
                1, 8, L.Settings.FormatAttackers, static p => p.FocusHurtCount, static (p, v) => p.FocusHurtCount = v),
        ]),
        new(L.Settings.FrontlineGroupRiding,
        [
            new(L.Settings.FrontlineMountDistance, L.Settings.FrontlineMountDistanceHelp,
                30, 120, L.Settings.FormatYards, static p => p.MountDistance, static (p, v) => p.MountDistance = v),
            new(L.Settings.FrontlineEnemyAware, L.Settings.FrontlineEnemyAwareHelp,
                10, 50, L.Settings.FormatYards, static p => p.EnemyAwareRadius, static (p, v) => p.EnemyAwareRadius = v),
        ]),
    ];

    public static void Draw(Configuration cfg)
    {
        var custom = cfg.CustomFrontline;
        foreach (var group in Groups)
        {
            DrawGroup(cfg, custom, group);
        }

        DrawResetRow(cfg);
    }

    private static void DrawGroup(Configuration cfg, CustomFrontlineProfile custom, Group group)
    {
        using var card = SettingsGroup.Begin(Loc.T(group.Title));
        foreach (var row in group.Rows)
        {
            DrawRow(cfg, custom, row);
        }
    }

    private static void DrawRow(Configuration cfg, CustomFrontlineProfile custom, Row row)
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
        if (PillButton.Draw("##cf_reset", resetLabel, Styling.AccentRose, emphasis, FontAwesomeIcon.Undo,
                tooltip: Loc.T(L.Settings.ResetDefaultsHelp))
            && armed)
        {
            cfg.CustomFrontline = new CustomFrontlineProfile();
            cfg.Save();
        }
    }
}
