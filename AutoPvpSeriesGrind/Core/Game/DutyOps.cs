using clib.Extensions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class DutyOps
{
    public static bool QueueCasualMatch() => Try("QueueCasualMatch", () =>
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
    }, fallback: false);

    public static bool IsQueued() => TrySilent(() =>
    {
        var cf = ContentsFinder.Instance();
        if (cf == null) return false;
        var qi = cf->GetQueueInfo();
        if (qi == null) return false;
        return qi->QueueState is ContentsFinderQueueState.Pending
            or ContentsFinderQueueState.Queued or ContentsFinderQueueState.Ready;
    }, fallback: false);

    public static void LeaveCurrentContent() => Try("LeaveCurrentContent", () =>
    {
        GameMain.ExecuteCommand((int)clib.Enums.CommandFlag.LeaveDuty, 0, 0, 0, 0);
        return true;
    }, fallback: false);

    public static int ContentTimeLeft() => TrySilent(() =>
    {
        var ef = EventFramework.Instance();
        if (ef == null) return 0;
        var dir = ef->GetInstanceContentDirector();
        if (dir == null || !dir->HasTimer()) return 0;
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0, dir->GetTimeRemaining(now));
    }, fallback: 0);

    private static T Try<T>(string label, Func<T> body, T fallback)
    {
        try { return body(); }
        catch (Exception ex) { ApsgLog.Warn(ex, $"{label} failed"); return fallback; }
    }

    private static T TrySilent<T>(Func<T> body, T fallback)
    {
        try { return body(); }
        catch { return fallback; }
    }
}
