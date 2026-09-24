using AutoPvpSeriesGrind.Core.Util;
using System.Runtime.CompilerServices;

namespace AutoPvpSeriesGrind.Core.Ipc;

internal static class IpcGate
{
    public static T Invoke<T>(bool hasFunction, Func<T> call, T fallback, string label, [CallerFilePath] string callerFile = "")
    {
        if (!hasFunction)
        {
            return fallback;
        }
        return Safe.Try(label, call, fallback, callerFile);
    }

    public static void Run(bool hasFunction, Action call, string label, [CallerFilePath] string callerFile = "")
    {
        if (!hasFunction)
        {
            return;
        }
        Safe.Try(label, call, callerFile);
    }
}
