using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class IntegrationSettings
{
    public static void Draw(Configuration cfg)
    {
        SettingsRow.Draw("Lifestream command", "Run this Lifestream command once before the first queue (e.g. travel to your preferred hub). Leave blank to disable. Requires Lifestream.",
            () =>
            {
                var v = cfg.LifestreamCommand ?? "";
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##lifestream", ref v, 128))
                {
                    cfg.LifestreamCommand = v;
                    cfg.SaveDebounced();
                }
            });

        SettingsRow.Draw("Follow-up command", "A chat command to run once the match limit is reached (e.g. /logout). Leave blank to disable.",
            () =>
            {
                var v = cfg.FollowUpCommand ?? "";
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##followup", ref v, 128))
                {
                    cfg.FollowUpCommand = v;
                    cfg.SaveDebounced();
                }
            });
    }
}
