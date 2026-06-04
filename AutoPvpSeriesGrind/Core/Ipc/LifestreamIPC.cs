using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.Ipc;

// Optional Lifestream integration for the startup "go to your PvP hub" command. Absent Lifestream, every
// call no-ops via IpcGate.
internal sealed class LifestreamIPC
{
    private static LifestreamIPC? instance;
    public static LifestreamIPC Instance => instance ??= new LifestreamIPC();

    private readonly ICallGateSubscriber<string, object> executeCommand;
    private readonly ICallGateSubscriber<bool> isBusy;

    private LifestreamIPC()
    {
        executeCommand = Svc.PluginInterface.GetIpcSubscriber<string, object>("Lifestream.ExecuteCommand");
        isBusy = Svc.PluginInterface.GetIpcSubscriber<bool>("Lifestream.IsBusy");
    }

    public bool IsAvailable => executeCommand.HasFunction;

    public void ExecuteCommand(string command)
        => IpcGate.Run(executeCommand.HasFunction, () => executeCommand.InvokeAction(command),
            "[LifestreamIPC] ExecuteCommand failed");

    public bool IsBusy()
        => IpcGate.Invoke(isBusy.HasFunction, isBusy.InvokeFunc, false, "[LifestreamIPC] IsBusy failed");
}
