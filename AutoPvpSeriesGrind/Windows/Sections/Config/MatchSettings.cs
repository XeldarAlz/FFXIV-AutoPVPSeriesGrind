using AutoPvpSeriesGrind.Core.Localization;
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
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupMatchIntro));

        SettingsRow.Draw(Loc.T(L.Settings.SayHello),
            Loc.T(L.Settings.SayHelloHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendHelloOnEntry, value => cfg.SendHelloOnEntry = value, "##mch_hello"),
            SettingsRow.ToggleHeight);

        if (cfg.SendHelloOnEntry)
        {
            SettingsRow.Draw(Loc.T(L.Settings.Chance),
                Loc.T(L.Settings.HelloChanceHelp),
                SettingsControls.RowSliderWidth,
                () => SettingsControls.DrawIntSlider(cfg, "##hellochance",
                    () => cfg.HelloChancePercent, value => cfg.HelloChancePercent = value, 0, 100, Loc.T(L.Settings.FormatPercentOfMatches)));

            SettingsRow.Draw(Loc.T(L.Settings.After),
                Loc.T(L.Settings.HelloAfterHelp),
                SettingsControls.RangeInlineWidth(),
                () => SettingsControls.DrawRangeInline(cfg, "##hellodelay_min", "##hellodelay_max",
                    () => cfg.HelloDelayMinSeconds, value => cfg.HelloDelayMinSeconds = value,
                    () => cfg.HelloDelayMaxSeconds, value => cfg.HelloDelayMaxSeconds = value, 30));
        }

        SettingsRow.Draw(Loc.T(L.Settings.Emotes),
            Loc.T(L.Settings.EmotesHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.RandomEmotes, value => cfg.RandomEmotes = value, "##mch_emotes"),
            SettingsRow.ToggleHeight);
    }

    private static void DrawResultsGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Settings.GroupResults));

        SettingsRow.Draw(Loc.T(L.Settings.GoodMatch),
            Loc.T(L.Settings.GoodMatchHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendGoodMatchOnResults, value => cfg.SendGoodMatchOnResults = value, "##mch_goodmatch"),
            SettingsRow.ToggleHeight);

        if (!cfg.SendGoodMatchOnResults)
        {
            return;
        }

        SettingsRow.Draw(Loc.T(L.Settings.Chance),
            Loc.T(L.Settings.GoodMatchChanceHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##gmchance",
                () => cfg.GoodMatchChancePercent, value => cfg.GoodMatchChancePercent = value, 0, 100, Loc.T(L.Settings.FormatPercentOfMatches)));

        SettingsRow.Draw(Loc.T(L.Settings.After),
            Loc.T(L.Settings.GoodMatchAfterHelp),
            SettingsControls.RangeInlineWidth(),
            () => SettingsControls.DrawRangeInline(cfg, "##gmdelay_min", "##gmdelay_max",
                () => cfg.GoodMatchDelayMinSeconds, value => cfg.GoodMatchDelayMinSeconds = value,
                () => cfg.GoodMatchDelayMaxSeconds, value => cfg.GoodMatchDelayMaxSeconds = value, 30));
    }
}
