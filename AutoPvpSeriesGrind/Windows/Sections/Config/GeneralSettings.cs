using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class GeneralSettings
{
    public static void Draw(Configuration cfg)
    {
        DrawLanguageGroup(cfg);
        DrawPacingGroup(cfg);
        DrawBreaksGroup(cfg);
        SettingsGroup.Footnote(Loc.T(L.Settings.SessionFootnote));
    }

    private static void DrawLanguageGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.Language));

        SettingsRow.Draw(Loc.T(L.Settings.Language),
            Loc.T(L.Settings.LanguageHelp),
            SettingsControls.RowComboWidth,
            () => SettingsControls.DrawLanguageCombo(cfg));
    }

    private static void DrawPacingGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupPacing));

        SettingsRow.Draw(Loc.T(L.Settings.LeaveDuty),
            Loc.T(L.Settings.LeaveDutyHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##leaveduty",
                () => cfg.LeaveDutyDelaySeconds, value => cfg.LeaveDutyDelaySeconds = value, 0, 30, Loc.T(L.Settings.FormatSeconds)));

        SettingsRow.Draw(Loc.T(L.Settings.Requeue),
            Loc.T(L.Settings.RequeueHelp),
            SettingsControls.RangeInlineWidth(),
            () => SettingsControls.DrawRangeInline(cfg, "##rq_Min", "##rq_Max",
                () => cfg.RequeueDelayMinSeconds, value => cfg.RequeueDelayMinSeconds = value,
                () => cfg.RequeueDelayMaxSeconds, value => cfg.RequeueDelayMaxSeconds = value, 60));
    }

    private static void DrawBreaksGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupBreaks));

        SettingsRow.Draw(Loc.T(L.Settings.TakeBreaks),
            Loc.T(L.Settings.TakeBreaksHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.TakeBreaks, value => cfg.TakeBreaks = value, "##ses_breaks"),
            SettingsRow.ToggleHeight);

        if (!cfg.TakeBreaks)
        {
            return;
        }

        SettingsRow.Draw(Loc.T(L.Settings.BreakEvery),
            Loc.T(L.Settings.BreakEveryHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##breakevery",
                () => cfg.BreakEveryMatches, value => cfg.BreakEveryMatches = value, 1, 100, Loc.T(L.Settings.FormatMatches)));

        SettingsRow.Draw(Loc.T(L.Settings.BreakLength),
            Loc.T(L.Settings.BreakLengthHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##breaklen",
                () => cfg.BreakMinutes, value => cfg.BreakMinutes = value, 1, 120, Loc.T(L.Settings.FormatMinutes)));
    }
}
