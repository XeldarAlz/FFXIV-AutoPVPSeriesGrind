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
        var cfg = plugin.Configuration;
        var ctrl = plugin.Controller;

        using var style = Styling.PushWindowStyle();

        Header.Draw(plugin, ctrl.Running);
        DependencyBanner.Draw(plugin);

        if (ctrl.Running)
            RunningPanel.Draw(cfg, ctrl);
        else
            IdlePanel.Draw(cfg, ctrl);

        Footer.Draw();
    }
}
