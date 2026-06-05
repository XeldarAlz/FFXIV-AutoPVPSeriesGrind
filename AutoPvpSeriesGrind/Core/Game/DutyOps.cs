using clib.Extensions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class DutyOps
{
    public static bool QueueCasualMatch()
    {
        try
        {
            var cf = ContentsFinder.Instance();
            if (cf == null) return false;
            var qi = cf->GetQueueInfo();
            if (qi == null) return false;

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
