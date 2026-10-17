using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Sections.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Pages;

internal sealed class SettingsPage
{
    private enum Tab { Session, Combat, Match, Shopping }

    private readonly record struct Entry(Tab Tab, LocString Label, FontAwesomeIcon Icon, LocString Subtitle);

    private static readonly Entry[] entries =
    [
        new(Tab.Session, L.Settings.CatSession, FontAwesomeIcon.Flag,        L.Settings.CatSessionSub),
        new(Tab.Combat,  L.Settings.CatCombat,  FontAwesomeIcon.Brain,       L.Settings.CatCombatSub),
        new(Tab.Match,   L.Settings.CatMatch,   FontAwesomeIcon.CommentDots, L.Settings.CatMatchSub),
        new(Tab.Shopping, L.Settings.CatShopping, FontAwesomeIcon.ShoppingCart, L.Settings.CatShoppingSub),
    ];

    private Tab activeTab = Tab.Session;
    private bool resetScroll;

    private static readonly Segmented.Item[] tabs = new Segmented.Item[4];

    public void ShowCombat()
    {
        activeTab = Tab.Combat;
        resetScroll = true;
    }

    public void Draw(Plugin plugin)
    {
        for (var index = 0; index < entries.Length; index++)
            tabs[index] = new(entries[index].Icon, Loc.T(entries[index].Label));
        var selected = (int)activeTab;
        if (Segmented.Draw("##settings_tabs", tabs, ref selected)) Select((Tab)selected);
        Styling.VSpace(14f);
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var content = ImRaii.Child("##apsg_settings_content", new Vector2(-1f, -1f), false, ImGuiWindowFlags.None);
        if (content) DrawContent(plugin.Configuration, plugin.Controller.Running);
    }

    private void Select(Tab tab)
    {
        if (activeTab == tab) return;
        activeTab = tab;
        resetScroll = true;
    }

    private void DrawContent(Configuration cfg, bool running)
    {
        if (resetScroll)
        {
            ImGui.SetScrollY(0f);
            resetScroll = false;
        }

        var entry = entries[(int)activeTab];
        var scale = ImGuiHelpers.GlobalScale;

        using var reveal = Motion.PushSwitch("##apsg_settings_tab", (int)activeTab);
        using var group = ImRaii.Group();
        ImGui.Dummy(new Vector2(0f, 2f * scale));
        PageHeader.Draw(Loc.T(entry.Label), Loc.T(entry.Subtitle));

        switch (activeTab)
        {
            case Tab.Session: GeneralSettings.Draw(cfg); break;
            case Tab.Combat: CombatSettings.Draw(cfg, running); break;
            case Tab.Match: MatchSettings.Draw(cfg); break;
            case Tab.Shopping: ShoppingSettings.Draw(cfg); break;
        }
    }
}
