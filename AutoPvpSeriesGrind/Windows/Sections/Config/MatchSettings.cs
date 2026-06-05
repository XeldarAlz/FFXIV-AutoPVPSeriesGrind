using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class MatchSettings
{
    public static void Draw(Configuration cfg)
    {
        SettingsRow.Draw("Say hello on entry", "Send /quickchat Hello once during the portrait/intro phase, at a random moment so it doesn't look scripted.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendHelloOnEntry, v => cfg.SendHelloOnEntry = v, "##hello"));

        if (cfg.SendHelloOnEntry)
        {
            SettingsRow.Draw("Greeting frequency", "How often the hello actually fires. Lower means it sometimes stays silent.",
                () => SettingsControls.DrawIntSlider(cfg, "##hellochance", () => cfg.HelloChancePercent, v => cfg.HelloChancePercent = v, 0, 100, "%d%% of matches", 220f));

            SettingsRow.Draw("Greeting delay", "How long after the portraits appear to wait before greeting — a random time in this range, so it never fires the instant the intro starts.",
                () => DrawDelayRange(cfg, "hellodelay", () => cfg.HelloDelayMinSeconds, v => cfg.HelloDelayMinSeconds = v, () => cfg.HelloDelayMaxSeconds, v => cfg.HelloDelayMaxSeconds = v));
        }

        SettingsRow.Draw("\"Good Match\" on results", "Send /quickchat \"Good Match\" when the results screen appears at the end of a match.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.SendGoodMatchOnResults, v => cfg.SendGoodMatchOnResults = v, "##goodmatch"));

        if (cfg.SendGoodMatchOnResults)
        {
            SettingsRow.Draw("Compliment frequency", "How often \"Good Match\" actually fires after a match.",
                () => SettingsControls.DrawIntSlider(cfg, "##gmchance", () => cfg.GoodMatchChancePercent, v => cfg.GoodMatchChancePercent = v, 0, 100, "%d%% of matches", 220f));

            SettingsRow.Draw("\"Good Match\" delay", "How long after the results screen appears to wait before sending \"Good Match\" — a random time in this range. If it lands later than the \"Delay before leaving the duty\" (under General), the bot leaves first and skips the goodbye.",
                () => DrawDelayRange(cfg, "gmdelay", () => cfg.GoodMatchDelayMinSeconds, v => cfg.GoodMatchDelayMinSeconds = v, () => cfg.GoodMatchDelayMaxSeconds, v => cfg.GoodMatchDelayMaxSeconds = v));
        }

        SettingsRow.Draw("Occasional emotes", "Sometimes play a friendly emote (wave, cheer, salute, thumbs-up) during the portrait phase. Adds a bit of personality.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.RandomEmotes, v => cfg.RandomEmotes = v, "##emotes"));
    }

    private static void DrawDelayRange(Configuration cfg, string id,
        Func<int> getMin, Action<int> setMin, Func<int> getMax, Action<int> setMax)
    {
        LabeledSlider(cfg, $"{id}_min", "Min", getMin, setMin);
        LabeledSlider(cfg, $"{id}_max", "Max", getMax, setMax);
    }

    private static void LabeledSlider(Configuration cfg, string id, string label, Func<int> get, Action<int> set)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(label);
        ImGui.SameLine(50f * ImGuiHelpers.GlobalScale);
        SettingsControls.DrawIntSlider(cfg, $"##{id}", get, set, 0, 30, "%d s", 220f);
    }
}
