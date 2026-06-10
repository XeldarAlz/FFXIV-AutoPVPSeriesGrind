using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class Header
{
    public static void Draw(Plugin plugin, bool running)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var drawList = ImGui.GetWindowDrawList();

        ImGui.AlignTextToFramePadding();
        var frameHeight = ImGui.GetFrameHeight();
        var origin = ImGui.GetCursorScreenPos();
        var dotRadius = 3.5f * scale;
        var dotColor = running
            ? Styling.PulseColor(Styling.AccentViolet, Styling.AccentVioletSoft, Styling.PulseCalm)
            : Styling.WithAlpha(Styling.AccentViolet, 0.5f);
        drawList.AddCircleFilled(new Vector2(origin.X + dotRadius, origin.Y + frameHeight * 0.5f), dotRadius, ImGui.GetColorU32(dotColor));

        ImGui.Dummy(new Vector2(dotRadius * 2 + 7 * scale, frameHeight));
        ImGui.SameLine(0, 0);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextStrong))
        {
            ImGui.TextUnformatted(Greeting());
        }

        DrawIconCluster(plugin, frameHeight);
        Styling.HairlineRule(2f, 2f);
    }

    private static string Greeting()
    {
        return DateTime.Now.Hour switch
        {
            >= 5 and < 12 => "Good morning, ready to grind?",
            >= 12 and < 17 => "Good afternoon, ready to grind?",
            >= 17 and < 22 => "Good evening, ready to grind?",
            _ => "Late night grind session?",
        };
    }

    private static void DrawIconCluster(Plugin plugin, float buttonSize)
    {
        var configuration = plugin.Configuration;
        var missing = !ExternalPlugins.AllRequiredInstalled();

        var buttonCount = configuration.EnableCombatBrain ? 5 : 4;
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var total = buttonCount * buttonSize + (buttonCount - 1) * spacing;
        ImGui.SameLine(ImGui.GetContentRegionAvail().X + ImGui.GetCursorPosX() - total);

        if (IconButton.Draw(FontAwesomeIcon.ChartLine, "##apsg_history", "Run history", null, buttonSize))
        {
            plugin.ToggleHistoryUi();
        }

        ImGui.SameLine();
        if (IconButton.Draw(FontAwesomeIcon.Plug, "##apsg_deps", missing ? "Required plugins missing" : "Dependencies",
            missing ? Styling.AccentRose : null, buttonSize))
        {
            plugin.ToggleDependenciesUi();
        }

        if (configuration.EnableCombatBrain)
        {
            ImGui.SameLine();
            if (IconButton.Draw(FontAwesomeIcon.Brain, "##apsg_brain", "Combat brain", LiveBrainDecisionTint(), buttonSize))
            {
                plugin.ToggleBrainUi();
            }
        }

        ImGui.SameLine();
        if (IconButton.Draw(FontAwesomeIcon.InfoCircle, "##apsg_about", "About", null, buttonSize))
        {
            plugin.ToggleAboutUi();
        }

        ImGui.SameLine();
        if (IconButton.Draw(FontAwesomeIcon.Cog, "##apsg_settings", "Settings", null, buttonSize))
        {
            plugin.ToggleConfigUi();
        }
    }

    private static Vector4? LiveBrainDecisionTint()
    {
        if (BrainTelemetry.IsFresh && BrainTelemetry.Plan is { } plan)
        {
            return plan.Kind switch
            {
                MoveKind.Retreat => Styling.AccentRose,
                MoveKind.Engage => Styling.AccentAmber,
                _ => Styling.AccentMint,
            };
        }

        return null;
    }
}
