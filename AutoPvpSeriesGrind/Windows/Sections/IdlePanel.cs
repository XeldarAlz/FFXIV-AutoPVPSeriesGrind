using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

// The idle (not-running) view: the one knob that matters (run-until goal), an at-a-glance summary of the
// active options, and the primary Start call-to-action. Kept lean — no hero banner — like the FATE plugin.
internal static class IdlePanel
{
    public static void Draw(Configuration cfg, Plugin plugin, AutoPvpSeriesController ctrl)
    {
        DrawHeaderRow(plugin);
        ImGui.Spacing();

        GoalSelector.Draw(cfg);
        ImGui.Spacing();
        DrawOptionChips(cfg);
        ImGui.Spacing();
        ImGui.Spacing();

        var ready = ExternalPlugins.AllRequiredInstalled();
        if (PrimaryButton.Draw("START", Styling.AccentViolet, ready))
            ctrl.Start();
        if (!ready)
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
                ImGui.TextWrapped("Install the required plugins first — open the dependencies window (the plug icon) to one-click them.");

        ImGui.Spacing();
        DrawFooter();
    }

    private static void DrawHeaderRow(Plugin plugin)
    {
        // Slim top strip: just the right-aligned toolbar icons. The Dummy anchors SameLine to this line.
        ImGui.AlignTextToFramePadding();
        ImGui.Dummy(new Vector2(1f, ImGui.GetFrameHeight()));
        TopToolbar.DrawIconsInline(plugin);
    }

    private static void DrawOptionChips(Configuration cfg)
    {
        Styling.SectionLabel("Active options");
        ImGui.Spacing();

        Chip("Hello on entry", cfg.SendHelloOnEntry);
        ImGui.SameLine();
        Chip("Good Match", cfg.SendGoodMatchOnResults);
        ImGui.SameLine();
        Chip("Garo titles", cfg.SetGaroTitles);
        ImGui.SameLine();
        Chip($"Gearset {(cfg.GearsetSlot > 0 ? cfg.GearsetSlot.ToString() : "off")}", cfg.GearsetSlot > 0);
        ImGui.SameLine();
        Chip("Lifestream", !string.IsNullOrWhiteSpace(cfg.LifestreamCommand));
    }

    private static void Chip(string label, bool on)
    {
        var s = ImGuiHelpers.GlobalScale;
        var padX = 9f * s;
        var textSize = ImGui.CalcTextSize(label);
        var size = new Vector2(textSize.X + padX * 2, ImGui.GetFrameHeight());
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var dl = ImGui.GetWindowDrawList();

        var accent = on ? Styling.AccentViolet : Styling.TextMuted;
        var bg = on ? Vector4.Lerp(Styling.CardBg, accent, 0.22f) : Styling.CardBgSoft;
        dl.AddRectFilled(origin, end, ImGui.GetColorU32(bg), size.Y * 0.5f);
        dl.AddRect(origin, end, ImGui.GetColorU32(on ? accent : Styling.BorderDim), size.Y * 0.5f);

        var textPos = new Vector2(origin.X + padX, origin.Y + (size.Y - textSize.Y) * 0.5f);
        ImGui.SetCursorScreenPos(textPos);
        using (ImRaii.PushColor(ImGuiCol.Text, on ? Styling.TextStrong : Styling.TextMuted))
            ImGui.TextUnformatted(label);

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(size);
    }

    private static void DrawFooter()
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextUnformatted($"{Core.ApsgConstants.PrimaryCommand} / {Core.ApsgConstants.AliasCommand}");
    }
}
