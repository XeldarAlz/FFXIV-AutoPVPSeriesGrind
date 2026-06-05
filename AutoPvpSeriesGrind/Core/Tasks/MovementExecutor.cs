using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using ECommons.Automation;
using System.Numerics;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

// Owns navmesh movement and the repath de-bounce state. Translates a MovePlan into vnav calls and
// avoids re-issuing a path to an unchanged destination every tick.
internal sealed class MovementExecutor
{
    private const float RepathThreshold = 2.5f;     // skip re-issuing a path if the new dest is this close to the last
    private const float PursuitRepathThreshold = 1f;
    private const float MinStopRange = 0.5f;         // below this we MoveTo exactly instead of MoveCloseTo
    private const float HoldSlack = 1.5f;            // extra slack before we bother re-pathing while holding

    // Minimum gap before re-pathing to an unchanged, not-yet-reached destination (avoids per-tick pathfind spam when stuck).
    private const int RepathCooldownMs = 2000;

    private static NavIpc Nav => NavIpc.Instance;

    private Vector3 lastMoveDest;
    private long lastMoveAtMs;
    private Posture? lastPosture;

    public void Reset()
    {
        lastMoveDest = default;
        lastMoveAtMs = 0;
        lastPosture = null;
    }

    // Records the latest posture and reports whether it changed since the previous tick — used to
    // gate the reaction delay so we only "hesitate" when actually switching tactics.
    public bool UpdatePosture(Posture posture)
    {
        var changed = lastPosture != posture;
        lastPosture = posture;
        return changed;
    }

    // Stops pathing without clearing the last destination (a transient pause).
    public void HaltPathing()
    {
        if (Nav.IsRunning()) Nav.Stop();
    }

    // Stops pathing and forgets the last destination so the next move always re-paths.
    public void Stop()
    {
        if (!Nav.IsRunning()) return;
        Nav.Stop();
        lastMoveDest = default;
    }

    public void Execute(in MovePlan plan)
    {
        if (plan.Sprint && !MatchState.HasStatus(StatusSprint))
            Chat.ExecuteCommand(GameCommands.Sprint);

        switch (plan.Kind)
        {
            case MoveKind.Hold:
                if (DistanceToSelf(plan.Destination) > plan.StopRange + HoldSlack)
                    IssueMove(plan.Destination, plan.Fallback, plan.StopRange);
                else
                    Stop();
                break;

            case MoveKind.Engage:
            case MoveKind.Retreat:
                IssueMove(plan.Destination, plan.Fallback, plan.StopRange, plan.Pursue);
                break;
        }
    }

    public void IssueMove(Vector3 dest, Vector3 fallback, float stopRange, bool pursue = false)
    {
        var drift = Vector3.Distance(dest, lastMoveDest);

        if (pursue)
        {
            if (drift < PursuitRepathThreshold)
            {
                if (Nav.IsRunning()) return;
                if (DistanceToSelf(dest) <= stopRange) return;
                if (Environment.TickCount64 - lastMoveAtMs < RepathCooldownMs) return;
            }
        }
        else if (drift < RepathThreshold)
        {
            if (Nav.IsRunning()) return;
            if (DistanceToSelf(dest) <= stopRange + RepathThreshold) return;
            if (Environment.TickCount64 - lastMoveAtMs < RepathCooldownMs) return;
        }

        lastMoveDest = dest;
        lastMoveAtMs = Environment.TickCount64;
        var target = Nav.NearestPointReachable(dest)
                     ?? (fallback != dest ? Nav.NearestPointReachable(fallback) : null)
                     ?? fallback;
        if (stopRange > MinStopRange) Nav.MoveCloseTo(target, stopRange);
        else Nav.MoveTo(target);
    }

    private static float DistanceToSelf(Vector3 p)
        => MatchState.PlayerPosition() is { } self ? Vector3.Distance(self, p) : float.MaxValue;
}
