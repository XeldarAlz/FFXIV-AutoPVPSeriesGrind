using ECommons.Automation;
using ECommons.DalamudServices;
using Dalamud.Plugin.Ipc;
using System.Numerics;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Ipc;

internal sealed class NavIpc
{
    private const float DefaultHalfExtentXZ = 5f;
    private const float DefaultHalfExtentY = 5f;

    private static NavIpc? instance;
    public static NavIpc Instance => instance ??= new NavIpc();

    private readonly ICallGateSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>> pathfind;
    private readonly ICallGateSubscriber<List<Vector3>, bool, object> followWaypoints;
    private readonly ICallGateSubscriber<object> stop;
    private readonly ICallGateSubscriber<bool> isRunning;
    private readonly ICallGateSubscriber<Vector3, float, float, Vector3?> nearestPointReachable;
    private readonly ICallGateSubscriber<bool> isReady;
    private readonly ICallGateSubscriber<float> buildProgress;

    private NavIpc()
    {
        pathfind = Svc.PluginInterface.GetIpcSubscriber<Vector3, Vector3, bool, Task<List<Vector3>>>(IpcGates.NavPathfind);
        followWaypoints = Svc.PluginInterface.GetIpcSubscriber<List<Vector3>, bool, object>(IpcGates.NavFollowWaypoints);
        stop = Svc.PluginInterface.GetIpcSubscriber<object>(IpcGates.NavStop);
        isRunning = Svc.PluginInterface.GetIpcSubscriber<bool>(IpcGates.NavIsRunning);
        nearestPointReachable = Svc.PluginInterface.GetIpcSubscriber<Vector3, float, float, Vector3?>(IpcGates.NavNearestPointReachable);
        isReady = Svc.PluginInterface.GetIpcSubscriber<bool>(IpcGates.NavIsReady);
        buildProgress = Svc.PluginInterface.GetIpcSubscriber<float>(IpcGates.NavBuildProgress);
    }

    public bool IsAvailable => pathfind.HasFunction && followWaypoints.HasAction;

    public Task<List<Vector3>>? Pathfind(Vector3 from, Vector3 to)
        => IpcGate.Invoke<Task<List<Vector3>>?>(pathfind.HasFunction, () => pathfind.InvokeFunc(from, to, false), null, "Nav.Pathfind failed");

    public void FollowWaypoints(List<Vector3> waypoints)
        => IpcGate.Run(followWaypoints.HasAction, () => followWaypoints.InvokeAction(waypoints, false), "Path.MoveTo failed");

    public void Stop()
    {
        if (stop.HasAction)
            IpcGate.Run(true, stop.InvokeAction, "Path.Stop failed");
        else
            Chat.ExecuteCommand(GameCommands.NavStop);
    }

    public bool IsFollowingPath()
        => IpcGate.Invoke(isRunning.HasFunction, isRunning.InvokeFunc, false, "IsRunning failed");

    // vnavmesh keeps the previous zone's mesh for a moment after a zone change, so a mesh that is
    // present but still (re)loading is not ready: its queries fault or wait for the whole build.
    public bool IsReady()
        => IpcGate.Invoke(isReady.HasFunction, isReady.InvokeFunc, false, "IsReady failed") && BuildProgress() < 0f;

    public float BuildProgress()
        => IpcGate.Invoke(buildProgress.HasFunction, buildProgress.InvokeFunc, -1f, "BuildProgress failed");

    public Vector3? NearestPointReachable(Vector3 point, float halfExtentXZ = DefaultHalfExtentXZ, float halfExtentY = DefaultHalfExtentY)
        => IpcGate.Invoke(nearestPointReachable.HasFunction,
            () => nearestPointReachable.InvokeFunc(point, halfExtentXZ, halfExtentY), (Vector3?)null,
            "NearestPointReachable failed");
}
