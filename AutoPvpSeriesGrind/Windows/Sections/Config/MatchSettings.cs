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

        SettingsRow.Draw("Set Garo titles on start", "Flip the Garo collaboration titles when the run starts. Leave off if you don't have them or don't want your title changed.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.SetGaroTitles, v => cfg.SetGaroTitles = v, "##garo"));
    }
}
