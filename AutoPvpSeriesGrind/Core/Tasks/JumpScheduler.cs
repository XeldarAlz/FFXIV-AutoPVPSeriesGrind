using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Humanize;
using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class JumpScheduler
{
    private const float TriggerYalms = 1.3f;
    private const float ShortcutDoneYalms = 1.5f;
    private const float HopMinTripYalms = 25f;
    private const float HopMinRemainingYalms = 10f;
    private const int HopMinDelayMs = 1500;
    private const int HopMaxDelayMs = 6000;
    private const int MinHopGapMs = 8000;
    private const float ShortcutProgressYalms = 0.5f;
    private const int ShortcutStallMs = 1000;

    private readonly Vector3[] edges = new Vector3[LedgeShortcut.MaxJumps];
    private int edgeCount;
    private int nextEdge;
    private long hopAtMs;
    private long lastHopAtMs;
    private float bestRemainingYalms;
    private long progressedAtMs;

    public bool ShortcutActive { get; private set; }
    public Vector3 ShortcutEnd { get; private set; }
    public bool ShortcutStalled { get; private set; }

    public void Reset()
    {
        ClearShortcut();
        hopAtMs = 0;
        lastHopAtMs = 0;
    }

    public void ClearShortcut()
    {
        ShortcutActive = false;
        ShortcutStalled = false;
        edgeCount = 0;
        nextEdge = 0;
    }

    public bool TryShortcut(Vector3 self, List<Vector3> waypoints, Func<Vector3, bool> rejected, long nowMs)
    {
        ClearShortcut();
        if (!LedgeShortcut.TryPlan(self, waypoints, rejected, edges, out edgeCount, out var skipCount))
        {
            return false;
        }

        ShortcutEnd = waypoints[skipCount];
        waypoints.RemoveRange(0, skipCount);
        ShortcutActive = true;
        bestRemainingYalms = VectorMath.HorizontalDistance(self, ShortcutEnd);
        progressedAtMs = nowMs;
        RunLog.Debug($"ledge shortcut to {ShortcutEnd:F0}, {edgeCount} jump(s), skipping {skipCount} waypoint(s)");
        return true;
    }

    public void PlanHop(float tripYalms, float hopChance, long nowMs)
    {
        if (hopAtMs != 0 || nowMs - lastHopAtMs < MinHopGapMs || tripYalms < HopMinTripYalms || !HumanTiming.Maybe(hopChance))
        {
            return;
        }
        hopAtMs = nowMs + HumanTiming.SharedRandom.Next(HopMinDelayMs, HopMaxDelayMs + 1);
    }

    public void Tick(Vector3 self, bool pathing, float remainingYalms, long nowMs)
    {
        if (ShortcutActive)
        {
            TickShortcut(self, nowMs);
        }
        if (hopAtMs == 0 || nowMs < hopAtMs)
        {
            return;
        }

        hopAtMs = 0;
        if (pathing && remainingYalms >= HopMinRemainingYalms && JumpOps.TryJump())
        {
            lastHopAtMs = nowMs;
            RunLog.Debug("hop while running");
        }
    }

    private void TickShortcut(Vector3 self, long nowMs)
    {
        var selfToEnd = VectorMath.HorizontalDistance(self, ShortcutEnd);
        if (selfToEnd <= ShortcutDoneYalms)
        {
            ClearShortcut();
            return;
        }
        if (selfToEnd < bestRemainingYalms - ShortcutProgressYalms)
        {
            bestRemainingYalms = selfToEnd;
            progressedAtMs = nowMs;
        }
        else if (nowMs - progressedAtMs > ShortcutStallMs)
        {
            ShortcutStalled = true;
            return;
        }
        if (nextEdge >= edgeCount)
        {
            return;
        }

        var edge = edges[nextEdge];
        if (selfToEnd < VectorMath.HorizontalDistance(edge, ShortcutEnd))
        {
            nextEdge++;
            return;
        }
        if (VectorMath.HorizontalDistance(self, edge) <= TriggerYalms && JumpOps.TryJump())
        {
            nextEdge++;
        }
    }
}
