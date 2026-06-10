using AutoPvpSeriesGrind.Windows.Sections;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    public MainWindow(Plugin plugin) : base("Auto PVP Series Grind###AutoPvpSeriesGrindMain")
    {
        this.plugin = plugin;
        Size = new Vector2(560, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.NoCollapse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(440, 470),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var configuration = plugin.Configuration;
        var controller = plugin.Controller;

        using var style = Styling.PushWindowStyle();

        Header.Draw(plugin, controller.Running);
        DependencyBanner.Draw(plugin);

        if (controller.Running)
            RunningPanel.Draw(configuration, controller);
        else
            IdlePanel.Draw(configuration, controller);
    }
}
