using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class MatchSettings
{
    private const string HelloDelayMinSliderId = "##hellodelay_min";
    private const string HelloDelayMaxSliderId = "##hellodelay_max";
    private const string GoodMatchDelayMinSliderId = "##gmdelay_min";
    private const string GoodMatchDelayMaxSliderId = "##gmdelay_max";

    public static void Draw(Configuration cfg)
    {
        SettingsRow.Draw("Say hello on entry", "Send /quickchat Hello once during the portrait/intro phase, at a random moment so it doesn't look scripted.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendHelloOnEntry, value => cfg.SendHelloOnEntry = value));

        if (cfg.SendHelloOnEntry)
        {
            SettingsRow.Draw("Greeting frequency", "How often the hello actually fires. Lower means it sometimes stays silent.",
                () => SettingsControls.DrawIntSlider(cfg, "##hellochance", () => cfg.HelloChancePercent, v => cfg.HelloChancePercent = v, 0, 100, "%d%% of matches", 220f));

            SettingsRow.Draw("Greeting delay", "How long after the portraits appear to wait before greeting — a random time in this range, so it never fires the instant the intro starts.",
                () => SettingsControls.DrawDelayRange(cfg, HelloDelayMinSliderId, HelloDelayMaxSliderId,
                    () => cfg.HelloDelayMinSeconds, value => cfg.HelloDelayMinSeconds = value,
                    () => cfg.HelloDelayMaxSeconds, value => cfg.HelloDelayMaxSeconds = value, 30));
        }

        SettingsRow.Draw("\"Good Match\" on results", "Send /quickchat \"Good Match\" when the results screen appears at the end of a match.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendGoodMatchOnResults, value => cfg.SendGoodMatchOnResults = value));

        if (cfg.SendGoodMatchOnResults)
        {
            SettingsRow.Draw("Compliment frequency", "How often \"Good Match\" actually fires after a match.",
                () => SettingsControls.DrawIntSlider(cfg, "##gmchance", () => cfg.GoodMatchChancePercent, v => cfg.GoodMatchChancePercent = v, 0, 100, "%d%% of matches", 220f));

            SettingsRow.Draw("\"Good Match\" delay", "How long after the results screen appears to wait before sending \"Good Match\" — a random time in this range. If it lands later than the \"Delay before leaving the duty\" (under General), the bot leaves first and skips the goodbye.",
                () => SettingsControls.DrawDelayRange(cfg, GoodMatchDelayMinSliderId, GoodMatchDelayMaxSliderId,
                    () => cfg.GoodMatchDelayMinSeconds, value => cfg.GoodMatchDelayMinSeconds = value,
                    () => cfg.GoodMatchDelayMaxSeconds, value => cfg.GoodMatchDelayMaxSeconds = value, 30));
        }

        SettingsRow.Draw("Occasional emotes", "Sometimes play a friendly emote (wave, cheer, salute, thumbs-up) during the portrait phase. Adds a bit of personality.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.RandomEmotes, value => cfg.RandomEmotes = value));
    }
}
