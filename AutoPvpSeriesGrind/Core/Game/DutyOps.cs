using AutoPvpSeriesGrind.Core.Util;
using clib.Extensions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class DutyOps
{
    // ContentsFinderQueueInfo.QueueRoulette's undocumented second parameter ("a3" in FFXIVClientStructs).
    private const byte QueueRouletteUnknownArgument = 0;

    private const int NoCommandArgument = 0;

    public static bool QueueCasualMatch() => Safe.Try("QueueCasualMatch failed", () =>
    {
        var contentsFinder = ContentsFinder.Instance();
        if (contentsFinder == null)
        {
            return false;
        }
        var queueInfo = contentsFinder->GetQueueInfo();
        if (queueInfo == null)
        {
            return false;
        }

        if (queueInfo->QueueState is ContentsFinderQueueState.Pending or ContentsFinderQueueState.Queued)
        {
            queueInfo->CancelQueue();
        }
        (*contentsFinder).ResetFlags();
        queueInfo->QueueRoulette(ApsgConstants.CasualMatchRouletteId, QueueRouletteUnknownArgument);
        return true;
    }, fallback: false);

    public static bool IsQueued() => Safe.TrySilent("IsQueued read failed", () =>
    {
        var contentsFinder = ContentsFinder.Instance();
        if (contentsFinder == null)
        {
            return false;
        }
        var queueInfo = contentsFinder->GetQueueInfo();
        if (queueInfo == null)
        {
            return false;
        }
        return queueInfo->QueueState is ContentsFinderQueueState.Pending
            or ContentsFinderQueueState.Queued or ContentsFinderQueueState.Ready;
    }, fallback: false);

    public static void LeaveCurrentContent() => Safe.Try("LeaveCurrentContent failed", () =>
    {
        GameMain.ExecuteCommand((int)clib.Enums.CommandFlag.LeaveDuty,
            NoCommandArgument, NoCommandArgument, NoCommandArgument, NoCommandArgument);
    });

    public static int ContentTimeLeft() => Safe.TrySilent("ContentTimeLeft read failed", () =>
    {
        var eventFramework = EventFramework.Instance();
        if (eventFramework == null)
        {
            return 0;
        }
        var contentDirector = eventFramework->GetInstanceContentDirector();
        if (contentDirector == null || !contentDirector->HasTimer())
        {
            return 0;
        }
        var now = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return Math.Max(0, contentDirector->GetTimeRemaining(now));
    }, fallback: 0);
}
