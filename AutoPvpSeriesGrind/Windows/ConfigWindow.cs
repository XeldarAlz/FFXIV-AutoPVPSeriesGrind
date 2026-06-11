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
    private enum Tab { Session, Combat, Match }

    private readonly record struct TabDescriptor(
        Tab Tab,
        string Label,
        FontAwesomeIcon Icon,
        string Title,
        string Subtitle,
        Action<Configuration> DrawSettings);

    private static readonly TabDescriptor[] TabDescriptors =
    {
        new(Tab.Session, "Session", FontAwesomeIcon.Flag, "Session",
            "How a run starts and when it stops.", GeneralSettings.Draw),
        new(Tab.Combat, "Combat", FontAwesomeIcon.Brain, "Combat",
            "How the bot positions and picks its fights.", CombatSettings.Draw),
        new(Tab.Match, "In match", FontAwesomeIcon.CommentDots, "In match",
            "The social touches the bot performs during each match.", MatchSettings.Draw),
    };

    private const float SidebarWidth = 168f;
    private const float HeaderFontScale = 1.55f;

    private readonly Plugin plugin;
    private Tab activeTab = Tab.Session;

    public ConfigWindow(Plugin plugin) : base("Auto PVP Series Grind - Settings###AutoPvpSeriesGrindConfig")
    {
        this.plugin = plugin;
        Size = new Vector2(620, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 400),
            MaximumSize = new Vector2(2000, 1600),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var configuration = plugin.Configuration;
        using var style = Styling.PushWindowStyle();

        var sidebarWidth = SidebarWidth * ImGuiHelpers.GlobalScale;

        using (ImRaii.Child("##cfg_sidebar", new Vector2(sidebarWidth, -1), border: false))
            DrawSidebar();

        ImGui.SameLine();

        using (ImRaii.Child("##cfg_content", new Vector2(-1, -1), border: false))
            DrawContent(configuration);
    }

    private void DrawSidebar()
    {
        ImGui.Spacing();
        for (var tabIndex = 0; tabIndex < TabDescriptors.Length; tabIndex++)
        {
            var descriptor = TabDescriptors[tabIndex];
            if (SidebarTab.Draw(descriptor.Label, descriptor.Icon, Styling.AccentViolet, activeTab == descriptor.Tab))
            {
                activeTab = descriptor.Tab;
            }
        }
    }

    private void DrawContent(Configuration configuration)
    {
        ImGui.Spacing();
        for (var tabIndex = 0; tabIndex < TabDescriptors.Length; tabIndex++)
        {
            var descriptor = TabDescriptors[tabIndex];
            if (descriptor.Tab != activeTab)
            {
                continue;
            }

            WindowHeader.Draw(descriptor.Title, descriptor.Subtitle, HeaderFontScale);
            descriptor.DrawSettings(configuration);
        }
    }
}
