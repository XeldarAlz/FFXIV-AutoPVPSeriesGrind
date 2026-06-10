using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class MatchSettings
{
    public static void Draw(Configuration cfg)
    {
        DrawIntroGroup(cfg);
        DrawResultsGroup(cfg);
    }

    private static void DrawIntroGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin("Match intro");

        SettingsRow.Draw("Say hello",
            "Sends /quickchat Hello once during the portrait phase, at a random moment so it doesn't look scripted.",
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendHelloOnEntry, value => cfg.SendHelloOnEntry = value),
            SettingsRow.ToggleHeight);

        if (cfg.SendHelloOnEntry)
        {
            SettingsRow.Draw("Chance",
                "How often the hello actually fires. Lower means it sometimes stays silent.",
                SettingsControls.RowSliderWidth,
                () => SettingsControls.DrawIntSlider(cfg, "##hellochance",
                    () => cfg.HelloChancePercent, value => cfg.HelloChancePercent = value, 0, 100, "%d%% of matches"));

            SettingsRow.Draw("After",
                "Waits a random time in this range after the portraits appear, so the greeting never fires the instant the intro starts.",
                SettingsControls.RangeInlineWidth(),
                () => SettingsControls.DrawRangeInline(cfg, "##hellodelay_min", "##hellodelay_max",
                    () => cfg.HelloDelayMinSeconds, value => cfg.HelloDelayMinSeconds = value,
                    () => cfg.HelloDelayMaxSeconds, value => cfg.HelloDelayMaxSeconds = value, 30));
        }

        SettingsRow.Draw("Occasional emotes",
            "Plays a random friendly emote (wave, cheer, salute, and the like) at a random moment of the pre-match countdown, sometimes twice. Waits until your character is free so the emote actually plays.",
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.RandomEmotes, value => cfg.RandomEmotes = value),
            SettingsRow.ToggleHeight);
    }

    private static void DrawResultsGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin("Results screen");

        SettingsRow.Draw("Say “Good Match”",
            "Sends /quickchat \"Good Match\" when the results screen appears at the end of a match.",
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendGoodMatchOnResults, value => cfg.SendGoodMatchOnResults = value),
            SettingsRow.ToggleHeight);

        if (!cfg.SendGoodMatchOnResults)
        {
            return;
        }

        SettingsRow.Draw("Chance",
            "How often \"Good Match\" actually fires after a match.",
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##gmchance",
                () => cfg.GoodMatchChancePercent, value => cfg.GoodMatchChancePercent = value, 0, 100, "%d%% of matches"));

        SettingsRow.Draw("After",
            "Waits a random time in this range after the results screen appears. If it lands later than \"Leave duty after\" (under Session), the bot leaves first and skips the goodbye.",
            SettingsControls.RangeInlineWidth(),
            () => SettingsControls.DrawRangeInline(cfg, "##gmdelay_min", "##gmdelay_max",
                () => cfg.GoodMatchDelayMinSeconds, value => cfg.GoodMatchDelayMinSeconds = value,
                () => cfg.GoodMatchDelayMaxSeconds, value => cfg.GoodMatchDelayMaxSeconds = value, 30));
    }
}
