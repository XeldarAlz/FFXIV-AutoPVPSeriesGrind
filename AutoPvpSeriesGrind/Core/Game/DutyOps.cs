using AutoPvpSeriesGrind.Core.Util;
using AutoPvpSeriesGrind.Core.Tasks;
using clib.Extensions;
using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
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

    private const byte DutyFinderPenaltyIndex = 0;
    private const byte InactivityPenaltyIndex = 1;

    private const int ResultsLeaveOpenEvent = -1;
    private const int ResultsLeaveRequestEvent = -2;

    public static bool QueueMatch(PvpQueueRoute route) => Safe.Try("QueueMatch failed", () =>
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

        if (queueInfo->QueueState != ContentsFinderQueueState.None || HasDutyConditions())
        {
            return false;
        }
        if (route.RouletteId == 0 && route.DutyId == 0) return false;
        (*contentsFinder).ResetFlags();
        if (route.DutyId != 0)
        {
            uint duty = route.DutyId;
            queueInfo->QueueDuties(&duty, 1);
        }
        else
        {
            queueInfo->QueueRoulette(route.RouletteId, QueueRouletteUnknownArgument);
        }
        return true;
    }, fallback: false);

    public static QueueState ReadQueueState() => Safe.TrySilent(() =>
    {
        var contentsFinder = ContentsFinder.Instance();
        if (contentsFinder == null)
        {
            return QueueState.Unknown;
        }
        var queueInfo = contentsFinder->GetQueueInfo();
        if (queueInfo == null)
        {
            return QueueState.Unknown;
        }
        return queueInfo->QueueState switch
        {
            ContentsFinderQueueState.None => HasDutyConditions() ? QueueState.Unknown : QueueState.Idle,
            ContentsFinderQueueState.Pending => QueueState.Registering,
            ContentsFinderQueueState.Queued => QueueState.Queued,
            ContentsFinderQueueState.Ready => QueueState.Ready,
            ContentsFinderQueueState.Accepted => QueueState.Accepted,
            ContentsFinderQueueState.InContent => QueueState.InDuty,
            _ => QueueState.Unknown,
        };
    }, fallback: QueueState.Unknown);

    public static bool IsQueued() => ReadQueueState() is QueueState.Registering or QueueState.Queued or QueueState.Ready or QueueState.Accepted;

    public static bool IsQueuedFor(PvpQueueRoute route) => Safe.TrySilent(() =>
    {
        var finder = ContentsFinder.Instance();
        if (finder == null) return false;
        var info = finder->GetQueueInfo();
        if (info == null) return false;
        if (route.RouletteId != 0) return route.Matches(info->QueuedContentRouletteId);
        if (info->QueuedContentRouletteId != 0) return false;
        foreach (var entry in info->QueuedEntries)
            if (route.Matches(info->QueuedContentRouletteId, entry.Id) && entry.ContentType == FFXIVClientStructs.FFXIV.Client.UI.Agent.ContentsType.Regular)
                return true;
        return false;
    }, fallback: false);

    private static bool HasDutyConditions()
        => MatchState.InDuty() || Svc.Condition[ConditionFlag.InDutyQueue]
        || Svc.Condition[ConditionFlag.WaitingForDuty] || AddonProbe.IsReady(ApsgConstants.AddonNames.DutyReady);

    public static bool HasDutyInFlight()
        => HasDutyConditions() || ReadQueueState() != QueueState.Idle;

    public static int QueuePenaltyMinutes() => Safe.TrySilent(() =>
    {
        var uiState = UIState.Instance();
        var dutyFinderMinutes = uiState->InstanceContent.GetPenaltyRemainingInMinutes(DutyFinderPenaltyIndex);
        var inactivityMinutes = uiState->InstanceContent.GetPenaltyRemainingInMinutes(InactivityPenaltyIndex);
        return (int)Math.Max(dutyFinderMinutes, inactivityMinutes);
    }, fallback: 0);

    public static bool WithdrawQueue() => Safe.Try("WithdrawQueue failed", () =>
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

        queueInfo->CancelQueue();
        return true;
    }, fallback: false);

    public static void LeaveCurrentContent() => Safe.Try("LeaveCurrentContent failed", () =>
    {
        GameMain.ExecuteCommand((int)clib.Enums.CommandFlag.LeaveDuty,
            NoCommandArgument, NoCommandArgument, NoCommandArgument, NoCommandArgument);
    });

    public static bool OpenResultsLeave() => FireResultsEvent(ResultsLeaveOpenEvent);

    public static bool RequestResultsLeave() => FireResultsEvent(ResultsLeaveRequestEvent);

    private static bool FireResultsEvent(int eventId) => Safe.Try("results screen leave failed", () =>
    {
        if (!AddonProbe.TryGetReady(ApsgConstants.AddonNames.MatchResults, out var addon))
        {
            return false;
        }

        Callback.Fire(addon, true, eventId);
        return true;
    }, fallback: false);

    public static bool TryConfirmLeavePrompt() => Safe.Try("leave prompt confirm failed", () =>
    {
        if (!AddonProbe.TryGetReady(ApsgConstants.AddonNames.SelectYesno, out var addon))
        {
            return false;
        }

        var prompt = new AddonMaster.SelectYesno((nint)addon);
        if (!GameText.IsLeaveDutyPrompt(prompt.Text))
        {
            return false;
        }

        prompt.Yes();
        return true;
    }, fallback: false);

    public static int ContentTimeLeft() => Safe.TrySilent(() =>
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
