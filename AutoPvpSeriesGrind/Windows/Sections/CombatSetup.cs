using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Sections.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class CombatSetup
{
    private static readonly ChoiceCards.Item[] Presets =
    [
        new(L.Settings.StrategyDefensive, L.Setup.DefensiveDetail, FontAwesomeIcon.ShieldAlt, Styling.AccentMint),
        new(L.Settings.StrategyModerate, L.Setup.ModerateDetail, FontAwesomeIcon.BalanceScale, Styling.AccentArc),
        new(L.Settings.StrategyAggressive, L.Setup.AggressiveDetail, FontAwesomeIcon.Bolt, Styling.AccentRose),
        new(L.Setup.Customize, L.Setup.CustomDetail, FontAwesomeIcon.SlidersH, Styling.AccentMagenta),
    ];
    private static readonly PvpStrategy[] Strategies =
        [PvpStrategy.Defensive, PvpStrategy.Moderate, PvpStrategy.Aggressive, PvpStrategy.Custom];

    public static bool Draw(Configuration cfg, bool showSettingsLink = true)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var title = Loc.T(L.Setup.CombatSetup);
        var titleSize = TextDraw.SectionTitleSize(title);
        TextDraw.SectionTitle(title, origin, Styling.TextStrong);
        var openSettings = false;
        var height = titleSize.Y;
        if (showSettingsLink)
        {
            var label = Loc.T(L.Setup.CombatSettings);
            var buttonWidth = PillButton.Width(label, FontAwesomeIcon.Cog);
            var inline = width > titleSize.X + buttonWidth + 20f * scale;
            ImGui.SetCursorScreenPos(inline ? new Vector2(origin.X + width - buttonWidth, origin.Y)
                : origin + new Vector2(0f, titleSize.Y + 6f * scale));
            openSettings = PillButton.Draw("##combat_settings", label, Styling.AccentArc, icon: FontAwesomeIcon.Cog);
            height = inline ? MathF.Max(height, 28f * scale) : titleSize.Y + 34f * scale;
        }
        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, height));
        ChoiceCards.Description(Loc.T(cfg.MatchType.IsLargeScale() ? L.Setup.SharedBehavior : L.Setup.ConflictBehavior));
        Styling.VSpace(6f);

        var strategy = cfg.MatchType.IsLargeScale() ? cfg.FrontlineStrategy : cfg.Strategy;
        var selected = !cfg.MatchType.IsLargeScale() && !cfg.EnableCombatBrain ? -1 : Array.IndexOf(Strategies, strategy);
        if (ChoiceCards.Draw("##combat_presets", Presets, ref selected, maxColumns: 4, minWidth: 165f, enabled: !cfg.ManualMovement))
        {
            if (cfg.MatchType.IsLargeScale()) cfg.FrontlineStrategy = Strategies[selected];
            else
            {
                cfg.EnableCombatBrain = true;
                cfg.Strategy = Strategies[selected];
            }
            cfg.SaveDebounced();
        }
        Styling.VSpace(6f);
        if (cfg.ManualMovement)
            ChoiceCards.Description(Loc.T(L.Setup.ManualBehavior), Styling.AccentAmberSoft);
        else if (selected < 0)
            ChoiceCards.Description(Loc.T(L.Setup.LegacyBehavior), Styling.AccentAmberSoft);
        else if (Strategies[selected] == PvpStrategy.Custom)
        {
            Styling.SectionLabel(Loc.T(L.Setup.CustomParameters));
            ChoiceCards.Description(Loc.T(L.Setup.CustomParametersHelp));
            Styling.VSpace(10f);
            if (cfg.MatchType.IsLargeScale()) CustomFrontlineSettings.Draw(cfg);
            else CustomStrategySettings.Draw(cfg);
        }
        return openSettings;
    }
}
