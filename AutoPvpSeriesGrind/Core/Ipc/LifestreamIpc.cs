using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.Ipc;

internal sealed class LifestreamIpc
{
    private static LifestreamIpc? instance;
    public static LifestreamIpc Instance => instance ??= new LifestreamIpc();

    private readonly ICallGateSubscriber<string, object> executeCommand;
    private readonly ICallGateSubscriber<bool> isBusy;

    private LifestreamIpc()
    {
        executeCommand = Svc.PluginInterface.GetIpcSubscriber<string, object>(ApsgConstants.IpcGates.LifestreamExecuteCommand);
        isBusy = Svc.PluginInterface.GetIpcSubscriber<bool>(ApsgConstants.IpcGates.LifestreamIsBusy);
    }

    public bool IsAvailable => executeCommand.HasFunction;

    public void ExecuteCommand(string command)
        => IpcGate.Run(executeCommand.HasFunction, () => executeCommand.InvokeAction(command),
            "[LifestreamIPC] ExecuteCommand failed");

    public bool IsBusy()
        => IpcGate.Invoke(isBusy.HasFunction, isBusy.InvokeFunc, false, "[LifestreamIPC] IsBusy failed");
}
