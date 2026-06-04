using AutoPvpSeriesGrind.Core.Game;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    private async Task Startup()
    {
        await NextFrame(1000);

        Controller_SetPhaseQueueing();
        DutyOps.QueueCasualMatch();
    }

    private void Controller_SetPhaseQueueing()
        => Plugin.Instance.Controller.Phase = AutoPhase.Queueing;

    // Out of duty: reset, stop if the limit was reached, otherwise (re)queue the casual roulette.
    private async Task<bool> TickOutOfDuty()
    {
        if (inMatchLive || announcedEntered || ranSafetyMoveThisDuty || baselineCaptured)
            ResetDutyState("out of duty");

        if (stopAfterCurrentMatch)
        {
            Diag("stop condition reached -> ending run");
            return true;
        }

        Plugin.Instance.Controller.Phase = AutoPhase.Queueing;

        // When a match pops, the game shows the "Duty Ready" popup and waits for us to press Commence.
        // The source script left this to an external plugin; here we click it ourselves, otherwise the
        // popup just times out and we never enter the instance. Poll for it during the requeue interval.
        if (await WaitUntilTimed(TryCommenceDuty, 5000, "duty-ready-commence", checkMs: 200))
        {
            Diag("duty ready popup -> commenced");
            await NextFrame(1000);
            return false;
        }

        if (!Svc.Condition[ConditionFlag.InDutyQueue] && !DutyOps.IsQueued())
        {
            Diag("not queued -> queueing casual match roulette");
            DutyOps.QueueCasualMatch();
        }

        await NextFrame(500);
        return false;
    }

    // True once the "Duty Ready" confirmation is up and its Commence button has been clicked. Returns false
    // when the popup is absent/not ready so the caller keeps polling until the match actually pops.
    private static unsafe bool TryCommenceDuty()
    {
        if (!GenericHelpers.TryGetAddonByName<AtkUnitBase>(ApsgConstants.AddonNames.DutyReady, out var a)
            || !GenericHelpers.IsAddonReady(a))
            return false;

        new AddonMaster.ContentsFinderConfirm((nint)a).Commence();
        return true;
    }
}
