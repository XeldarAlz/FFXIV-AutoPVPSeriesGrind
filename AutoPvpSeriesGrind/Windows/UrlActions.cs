using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using ECommons.DalamudServices;
using System.Diagnostics;

namespace AutoPvpSeriesGrind.Windows;

internal static class UrlActions
{
    public static void OpenInBrowser(string url, Action<Exception>? onError = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ImGui.SetClipboardText(url);
            onError?.Invoke(ex);
        }
    }

    public static void OpenOrCopy(string url)
        => OpenInBrowser(url, exception =>
            Svc.Log.Warning(exception, $"failed to launch browser for {url}, copied to clipboard instead"));

    public static void HoveredLinkInteraction(string url, string tooltip)
    {
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        using (ImRaii.Tooltip())
            ImGui.TextUnformatted(tooltip);
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            OpenOrCopy(url);
        }
        else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
        {
            ImGui.SetClipboardText(url);
        }
    }
}
