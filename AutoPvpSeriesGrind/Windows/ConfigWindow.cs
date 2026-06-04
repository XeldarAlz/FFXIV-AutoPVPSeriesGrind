using AutoPvpSeriesGrind.Windows.Components;
using AutoPvpSeriesGrind.Windows.Sections.Config;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private enum Tab { Session, Match }

    private readonly Plugin plugin;
    private Tab activeTab = Tab.Session;

    public ConfigWindow(Plugin plugin) : base("Auto PVP Series Grind — Settings###AutoPvpSeriesGrindConfig")
    {
        this.plugin = plugin;
        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(560, 460);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 360),
            MaximumSize = new Vector2(2000, 1600),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var cfg = plugin.Configuration;
        using var style = Styling.PushWindowStyle();

        var sidebarWidth = 168f * ImGuiHelpers.GlobalScale;

        using (ImRaii.Child("##cfg_sidebar", new Vector2(sidebarWidth, -1), border: false))
            DrawSidebar();

        ImGui.SameLine();

        using (ImRaii.Child("##cfg_content", new Vector2(-1, -1), border: false))
            DrawContent(cfg);
    }

    private void DrawSidebar()
    {
        ImGui.Spacing();
        if (SidebarTab.Draw("Session", FontAwesomeIcon.Flag, Styling.AccentViolet, activeTab == Tab.Session)) activeTab = Tab.Session;
        if (SidebarTab.Draw("In match", FontAwesomeIcon.CommentDots, Styling.AccentViolet, activeTab == Tab.Match)) activeTab = Tab.Match;
    }

    private void DrawContent(Configuration cfg)
    {
        ImGui.Spacing();
        switch (activeTab)
        {
            case Tab.Session:
                DrawHeader("Session", "How a run starts and when it stops.");
                GeneralSettings.Draw(cfg);
                break;
            case Tab.Match:
                DrawHeader("In match", "The social touches the bot performs during each match.");
                MatchSettings.Draw(cfg);
                break;
        }
    }

    private static void DrawHeader(string title, string subtitle)
    {
        ImGui.SetWindowFontScale(1.55f);
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
            ImGui.TextUnformatted(title);
        ImGui.SetWindowFontScale(1.0f);

        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted(subtitle);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
    }
}
