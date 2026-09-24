using ECommons.DalamudServices;
using System.Runtime.CompilerServices;

namespace AutoPvpSeriesGrind.Core;

// Chat lines never reach dalamud.log, so each one is recorded in the console to keep bug reports complete.
internal static class ApsgChat
{
    public static void Print(string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Chat.Print($"{ApsgConstants.LogPrefix} {message}");
        RunLog.Record(RunLogLevel.Info, message, callerFile);
    }

    public static void PrintError(string message, [CallerFilePath] string callerFile = "")
    {
        Svc.Chat.PrintError($"{ApsgConstants.LogPrefix} {message}");
        RunLog.Record(RunLogLevel.Error, message, callerFile);
    }
}
