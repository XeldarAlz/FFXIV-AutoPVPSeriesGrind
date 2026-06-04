using AutoPvpSeriesGrind.Windows.Components;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

// "In match" tab — the social touches the bot performs during each match.
internal static class MatchSettings
{
    public static void Draw(Configuration cfg)
    {
        SettingsRow.Draw("Say hello on entry", "Send /quickchat Hello once during the portrait/intro phase, at a random moment so it doesn't look scripted.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendHelloOnEntry, v => cfg.SendHelloOnEntry = v, "##hello"));

        SettingsRow.Draw("\"Good Match\" on results", "Send /quickchat \"Good Match\" when the results screen appears at the end of a match.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendGoodMatchOnResults, v => cfg.SendGoodMatchOnResults = v, "##goodmatch"));
    }
}
