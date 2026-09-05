using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
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
        catch (Exception exception)
        {
            ImGui.SetClipboardText(url);
            onError?.Invoke(exception);
        }
    }

    public static void OpenOrCopy(string url)
        => OpenInBrowser(url, exception =>
            Svc.Log.Warning(exception, $"failed to launch browser for {url}, copied to clipboard instead"));

    public static void HoveredLinkInteraction(string url, string tooltip)
    {
        ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        Tooltip.Show(tooltip);
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
