using AutoPvpSeriesGrind.Core.Lb;
using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.Ipc;

internal sealed class PvpAutoLbIpc
{
    private static PvpAutoLbIpc? instance;
    public static PvpAutoLbIpc Instance => instance ??= new PvpAutoLbIpc();

    private readonly ICallGateSubscriber<int> apiVersion;
    private readonly ICallGateSubscriber<int> getVersion;
    private readonly ICallGateSubscriber<string, int, bool> apply;

    private PvpAutoLbIpc()
    {
        apiVersion = Svc.PluginInterface.GetIpcSubscriber<int>(ApsgConstants.IpcGates.PvpAutoLbApiVersion);
        getVersion = Svc.PluginInterface.GetIpcSubscriber<int>(ApsgConstants.IpcGates.PvpAutoLbGetVersion);
        apply = Svc.PluginInterface.GetIpcSubscriber<string, int, bool>(ApsgConstants.IpcGates.PvpAutoLbApply);
    }

    public void PushPresetsIfNeeded()
    {
        if (!apply.HasFunction)
        {
            ApsgLog.Info("PvpAutoLb preset IPC not available; skipping push.");
            return;
        }

        var api = IpcGate.Invoke(apiVersion.HasFunction, apiVersion.InvokeFunc, 0, "PvpAutoLbIpc: ApiVersion failed");
        if (api != ApsgConstants.PvpAutoLbPresetApiVersion)
        {
            ApsgLog.Warn($"PvpAutoLb preset API mismatch (theirs {api}, ours {ApsgConstants.PvpAutoLbPresetApiVersion}); skipping push.");
            return;
        }

        var current = IpcGate.Invoke(getVersion.HasFunction, getVersion.InvokeFunc, -1, "PvpAutoLbIpc: GetVersion failed");
        if (current == LbPresets.Version)
            return;

        var ok = IpcGate.Invoke(apply.HasFunction, () => apply.InvokeFunc(LbPresets.ToJson(), LbPresets.Version), false,
            "PvpAutoLbIpc: Apply failed");
        ApsgLog.Info($"pushed LB presets v{LbPresets.Version} to PvpAutoLb (was v{current}): {(ok ? "ok" : "failed")}");
    }
}
