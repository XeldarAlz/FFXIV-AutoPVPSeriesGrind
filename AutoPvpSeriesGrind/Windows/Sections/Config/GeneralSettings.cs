using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class GeneralSettings
{
    public static void Draw(Configuration cfg)
    {
        DrawPacingGroup(cfg);
        DrawBreaksGroup(cfg);
        SettingsGroup.Footnote("Run length and what happens afterwards are set on the main window, under “Run until”.");
    }

    private static void DrawPacingGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin("Pacing");

        SettingsRow.Draw("Leave duty after",
            "Once the results screen appears, wait this long before leaving the duty. 0 leaves as soon as the match ends.",
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##leaveduty",
                () => cfg.LeaveDutyDelaySeconds, value => cfg.LeaveDutyDelaySeconds = value, 0, 30, "%d s"));

        SettingsRow.Draw("Re-queue after",
            "Waits a random time in this range before queueing the next match; re-queueing the instant a match ends looks robotic. 0 – 0 queues immediately.",
            SettingsControls.RangeInlineWidth(),
            () => SettingsControls.DrawRangeInline(cfg, "##rq_Min", "##rq_Max",
                () => cfg.RequeueDelayMinSeconds, value => cfg.RequeueDelayMinSeconds = value,
                () => cfg.RequeueDelayMaxSeconds, value => cfg.RequeueDelayMaxSeconds = value, 60));
    }

    private static void DrawBreaksGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin("Breaks");

        SettingsRow.Draw("Take breaks",
            "Idle for a while every so often, the way a person steps away between sessions.",
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.TakeBreaks, value => cfg.TakeBreaks = value),
            SettingsRow.ToggleHeight);

        if (!cfg.TakeBreaks)
        {
            return;
        }

        SettingsRow.Draw("Break every",
            "How many matches between breaks.",
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##breakevery",
                () => cfg.BreakEveryMatches, value => cfg.BreakEveryMatches = value, 1, 100, "%d matches"));

        SettingsRow.Draw("Break length",
            "Roughly how long each break lasts, varied by ±20% each time.",
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##breaklen",
                () => cfg.BreakMinutes, value => cfg.BreakMinutes = value, 1, 120, "%d min"));
    }
}
