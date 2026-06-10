using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core;

internal static class ApsgLog
{
    public static void Debug(string message) => Svc.Log.Debug(Prefix(message));
    public static void Info(string message) => Svc.Log.Info(Prefix(message));
    public static void Warn(string message) => Svc.Log.Warning(Prefix(message));
    public static void Warn(Exception ex, string message) => Svc.Log.Warning(ex, Prefix(message));

    public static void Chat(string message) => Svc.Chat.Print(Prefix(message));
    public static void ChatError(string message) => Svc.Chat.PrintError(Prefix(message));

    private static string Prefix(string message) => $"{ApsgConstants.LogPrefix} {message}";
}
