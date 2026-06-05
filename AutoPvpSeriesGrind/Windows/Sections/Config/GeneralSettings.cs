using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class GeneralSettings
{
    public static void Draw(Configuration cfg)
    {
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            ImGui.TextWrapped("The stop condition (matches / Series rank / time / endless) and what to do afterwards live on the main window under \"Run until\".");
        ImGui.Spacing();
        ImGui.Spacing();

        Styling.SectionLabel("Pacing");
        ImGui.Spacing();

        SettingsRow.Draw("Delay before leaving the duty",
            "Once the results screen appears, wait this long before leaving the duty. Set to 0 to leave as soon as the match ends.",
            () => SettingsControls.DrawIntSlider(cfg, "##leaveduty", () => cfg.LeaveDutyDelaySeconds, v => cfg.LeaveDutyDelaySeconds = v, 0, 30, "%d s", 220f));

        SettingsRow.Draw("Delay before re-queueing",
            "After a match ends, wait a random time in this range before queueing the next one — re-queueing the instant a match ends looks robotic. Set both to 0 to queue immediately.",
            () => DrawRequeueRange(cfg));

        SettingsRow.Draw("Take breaks",
            "Idle for a while every so often, the way a person steps away between sessions. Off by default.",
            () => SettingsControls.DrawToggle(cfg, () => cfg.TakeBreaks, v => cfg.TakeBreaks = v, "##breaks"));

        if (cfg.TakeBreaks)
        {
            SettingsRow.Draw("Break every",
                "How many matches between breaks.",
                () => SettingsControls.DrawIntSlider(cfg, "##breakevery", () => cfg.BreakEveryMatches, v => cfg.BreakEveryMatches = v, 1, 100, "%d matches", 220f));

            SettingsRow.Draw("Break length",
                "Roughly how long each break lasts (varied by ±20% each time).",
                () => SettingsControls.DrawIntSlider(cfg, "##breaklen", () => cfg.BreakMinutes, v => cfg.BreakMinutes = v, 1, 120, "%d min", 220f));
        }
    }

    private static void DrawRequeueRange(Configuration cfg)
    {
        LabeledSlider(cfg, "Min", () => cfg.RequeueDelayMinSeconds, v => cfg.RequeueDelayMinSeconds = v);
        LabeledSlider(cfg, "Max", () => cfg.RequeueDelayMaxSeconds, v => cfg.RequeueDelayMaxSeconds = v);
    }

    private static void LabeledSlider(Configuration cfg, string label, Func<int> get, Action<int> set)
    {
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextSecondary))
            ImGui.TextUnformatted(label);
        ImGui.SameLine(50f * ImGuiHelpers.GlobalScale);
        SettingsControls.DrawIntSlider(cfg, $"##rq_{label}", get, set, 0, 60, "%d s", 220f);
    }
}
