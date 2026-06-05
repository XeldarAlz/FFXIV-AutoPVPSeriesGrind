using ECommons.Automation;
using ECommons.DalamudServices;
using Dalamud.Plugin.Ipc;
using System.Numerics;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Ipc;

internal sealed class NavIpc
{
    private static NavIpc? instance;
    public static NavIpc Instance => instance ??= new NavIpc();

    private readonly ICallGateSubscriber<Vector3, bool, bool> moveTo;
    private readonly ICallGateSubscriber<Vector3, bool, float, bool> moveCloseTo;
    private readonly ICallGateSubscriber<object> stop;
    private readonly ICallGateSubscriber<bool> isRunning;
    private readonly ICallGateSubscriber<bool> pathfindInProgress;
    private readonly ICallGateSubscriber<bool> navIsReady;
    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPointReachable;

    private NavIpc()
    {
        moveTo = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, bool>(IpcGates.NavMoveTo);
        moveCloseTo = Svc.PluginInterface.GetIpcSubscriber<Vector3, bool, float, bool>(IpcGates.NavMoveCloseTo);
        stop = Svc.PluginInterface.GetIpcSubscriber<object>(IpcGates.NavStop);
        isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>(IpcGates.NavIsRunning);
        pathfindInProgress = Svc.PluginInterface.GetIpcSubscriber<bool>(IpcGates.NavPathfindInProgress);
        navIsReady = Svc.PluginInterface.GetIpcSubscriber<bool>(IpcGates.NavIsReady);
        nearestPointReachable = Svc.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>(IpcGates.NavNearestPointReachable);
    }

    public bool IsAvailable => moveTo.HasFunction;

    public void MoveTo(Vector3 dest, bool fly = false)
    {
        if (moveTo.HasFunction)
            IpcGate.Run(true, () => { _ = moveTo.InvokeFunc(dest, fly); }, "NavIpc: PathfindAndMoveTo failed");
        else
            Chat.ExecuteCommand(GameCommands.NavMoveTo(dest));
    }

    public void MoveCloseTo(Vector3 dest, float range, bool fly = false)
    {
        if (moveCloseTo.HasFunction)
            IpcGate.Run(true, () => { _ = moveCloseTo.InvokeFunc(dest, fly, range); }, "NavIpc: PathfindAndMoveCloseTo failed");
        else
            Chat.ExecuteCommand(GameCommands.NavMoveTo(dest));
    }

    public void Stop()
    {
        if (stop.HasFunction)
            IpcGate.Run(true, stop.InvokeAction, "NavIpc: Path.Stop failed");
        else
            Chat.ExecuteCommand(GameCommands.NavStop);
    }

    public bool IsRunning()
        => IpcGate.Invoke(isRunning.HasFunction, isRunning.InvokeFunc, false, "NavIpc: IsRunning failed")
        || IpcGate.Invoke(pathfindInProgress.HasFunction, pathfindInProgress.InvokeFunc, false, "NavIpc: PathfindInProgress failed");

    public bool IsReady() => IpcGate.Invoke(navIsReady.HasFunction, navIsReady.InvokeFunc, true, "NavIpc: IsReady failed");

    public Vector3? NearestPointReachable(Vector3 point, float halfExtentXZ = 5f, float halfExtentY = 5f)
        => IpcGate.Invoke(nearestPointReachable.HasFunction,
            () => nearestPointReachable.InvokeFunc(point, halfExtentXZ, halfExtentY), (Vector3?)null,
            "NavIpc: NearestPointReachable failed");
}
