using clib.Extensions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoPvpSeriesGrind.Core.Game;

// The three native game operations the source script reached for via SND's high-level wrappers
// (Instances.DutyFinder, InstancedContent). Each is a thin, guarded call against FFXIVClientStructs so a
// bad pointer/state logs and degrades rather than throwing into the loop. The exact behaviour of these
// should be confirmed in-game (queue/leave/timer) — see README.
internal static unsafe class DutyOps
{
    // Queue the Crystalline Conflict (Casual Match) roulette, matching the script's QueueRoulette(40).
    public static bool QueueCasualMatch()
    {
        try
        {
            var cf = ContentsFinder.Instance();
            if (cf == null) return false;
            var qi = cf->GetQueueInfo();
            if (qi == null) return false;

            // Clear any stale request, reset duty-finder flags, then register the roulette.
            if (qi->QueueState is ContentsFinderQueueState.Pending or ContentsFinderQueueState.Queued)
                qi->CancelQueue();
            (*cf).ResetFlags();
            qi->QueueRoulette(ApsgConstants.CasualMatchRouletteId, 0);
            return true;
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"{ApsgConstants.LogPrefix} QueueCasualMatch failed");
            return false;
        }
    }

    public static bool IsQueued()
    {
        try
        {
            var cf = ContentsFinder.Instance();
            if (cf == null) return false;
            var qi = cf->GetQueueInfo();
            if (qi == null) return false;
            return qi->QueueState is ContentsFinderQueueState.Pending
                or ContentsFinderQueueState.Queued or ContentsFinderQueueState.Ready;
        }
        catch { return false; }
    }

    // Leave the current instanced content (the script's InstancedContent.LeaveCurrentContent). 819 is the
    // game's "leave duty" ExecuteCommand opcode (clib.Enums.CommandFlag.LeaveDuty).
    public static void LeaveCurrentContent()
    {
        try
        {
            GameMain.ExecuteCommand((int)clib.Enums.CommandFlag.LeaveDuty, 0, 0, 0, 0);
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, $"{ApsgConstants.LogPrefix} LeaveCurrentContent failed");
        }
    }

    // Seconds left on the instance content director's timer, or 0 when there is no active timer. Drives the
    // portrait/gate detection just like the script's InstancedContent.ContentTimeLeft.
    public static int ContentTimeLeft()
    {
        try
        {
            var ef = EventFramework.Instance();
            if (ef == null) return 0;
            var dir = ef->GetInstanceContentDirector();
            if (dir == null || !dir->HasTimer()) return 0;
            var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Math.Max(0, dir->GetTimeRemaining(now));
        }
        catch { return 0; }
    }
}
