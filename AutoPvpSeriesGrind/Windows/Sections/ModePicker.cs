using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class ModePicker
{
    private static readonly ChoiceCards.Item[] Modes =
    [
        new(L.Setup.CrystallineConflict, L.Setup.ConflictDetail, FontAwesomeIcon.Gem, Styling.AccentArc),
        new(L.Plan.ModeFrontline, L.Setup.FrontlineDetail, FontAwesomeIcon.Flag, Styling.AccentMint),
        new(L.Plan.ModeRivalWings, L.Setup.WingsDetail, FontAwesomeIcon.ChessRook, Styling.AccentAmber),
    ];
    private static readonly Segmented.Item[] Queues = new Segmented.Item[2];

    public static void Draw(Configuration cfg, bool enabled = true)
    {
        Styling.SectionLabel(Loc.T(L.Setup.ChooseMode));
        Styling.VSpace(6f);
        var selected = (int)cfg.MatchType;
        if (ChoiceCards.Draw("##pvp_modes", Modes, ref selected, enabled: enabled))
        {
            cfg.MatchType = (MatchType)selected;
            cfg.SaveDebounced();
        }
        Styling.VSpace(6f);
        if (cfg.MatchType == MatchType.CrystallineConflict)
        {
            Queues[0] = new(FontAwesomeIcon.Gem, Loc.T(L.Setup.Casual));
            Queues[1] = new(FontAwesomeIcon.Trophy, Loc.T(L.Setup.Ranked));
            var queue = cfg.RankedCrystallineConflict ? 1 : 0;
            if (Segmented.Draw("##cc_queue", Queues, ref queue, enabled, height: 34f))
            {
                cfg.RankedCrystallineConflict = queue == 1;
                cfg.SaveDebounced();
            }
        }
        else if (cfg.MatchType == MatchType.RivalWings)
            ChoiceCards.Description(Loc.T(L.Plan.QueueRivalWingsHelp), Styling.AccentAmberSoft);
    }
}
