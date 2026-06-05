using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.Ipc;

internal static class IpcGate
{
    public static T Invoke<T>(bool hasFunction, Func<T> call, T fallback, string label)
    {
        if (!hasFunction) return fallback;
        try { return call(); }
        catch (Exception ex) { Svc.Log.Warning(ex, label); return fallback; }
    }

    public static void Run(bool hasFunction, Action call, string label)
    {
        if (!hasFunction) return;
        try { call(); }
        catch (Exception ex) { Svc.Log.Warning(ex, label); }
    }
}
