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
    private enum Tab { Session, Combat, Match }

    private readonly record struct Entry(Tab Tab, LocString Label, FontAwesomeIcon Icon, LocString Subtitle);

    private static readonly Entry[] entries =
    [
        new(Tab.Session, L.Settings.CatSession, FontAwesomeIcon.Flag,        L.Settings.CatSessionSub),
        new(Tab.Combat,  L.Settings.CatCombat,  FontAwesomeIcon.Brain,       L.Settings.CatCombatSub),
        new(Tab.Match,   L.Settings.CatMatch,   FontAwesomeIcon.CommentDots, L.Settings.CatMatchSub),
    ];

    private Tab activeTab = Tab.Session;
    private bool resetScroll;

    public void Draw(Plugin plugin)
    {
        var cfg = plugin.Configuration;
        var scale = ImGuiHelpers.GlobalScale;
        var navWidth = Layout.SettingsNavWidth * scale;

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero))
        {
            using (var nav = ImRaii.Child("##apsg_settings_nav", new Vector2(navWidth, -1f), false, ImGuiWindowFlags.NoScrollbar))
            {
                if (nav) DrawNav();
            }

            ImGui.SameLine(0f, 18f * scale);

            using (var content = ImRaii.Child("##apsg_settings_content", new Vector2(-1f, -1f), false, ImGuiWindowFlags.None))
            {
                if (content) DrawContent(cfg);
            }
        }
    }

    private void DrawNav()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var title = Loc.T(L.Settings.Title);
        using (Fonts.PushTitle())
        {
            TextDraw.At(title, new Vector2(origin.X + 6f * scale, origin.Y), Styling.TextStrong);
            ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, TextDraw.Measure(title).Y + 10f * scale));
        }

        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            if (SidebarTab.Draw(Loc.T(entry.Label), entry.Icon, Styling.AccentViolet, activeTab == entry.Tab)) Select(entry.Tab);
        }
    }

    private void Select(Tab tab)
    {
        if (activeTab == tab) return;
        activeTab = tab;
        resetScroll = true;
    }

    private void DrawContent(Configuration cfg)
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
            case Tab.Combat: CombatSettings.Draw(cfg); break;
            case Tab.Match: MatchSettings.Draw(cfg); break;
        }
    }
}
