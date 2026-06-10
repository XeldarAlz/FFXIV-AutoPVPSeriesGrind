using AutoPvpSeriesGrind.Core.Util;

namespace AutoPvpSeriesGrind.Core.Ipc;

internal static class IpcGate
{
    public static T Invoke<T>(bool hasFunction, Func<T> call, T fallback, string label)
    {
        if (!hasFunction)
        {
            return fallback;
        }
        return Safe.Try(label, call, fallback);
    }

    public static void Run(bool hasFunction, Action call, string label)
    {
        if (!hasFunction)
        {
            return;
        }
        Safe.Try(label, call);
    }
}
