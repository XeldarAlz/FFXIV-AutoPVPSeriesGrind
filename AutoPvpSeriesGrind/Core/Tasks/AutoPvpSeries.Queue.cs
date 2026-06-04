using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    private const int StartupZoneWaitMs = 300_000;

    private async Task Startup()
    {
        if (setGaroTitles)
        {
            Diag($"setting titles: {ApsgConstants.GaroTitle1} -> {ApsgConstants.GaroTitle2}");
            Cmd($"/title set {ApsgConstants.GaroTitle1}");
            await NextFrame(3000);
            Cmd($"/title set {ApsgConstants.GaroTitle2}");
            await NextFrame(3000);
        }

        GearsetOps.EquipSlot(gearsetSlot);
        await NextFrame(1000);

        await RunStartupLifestream();

        Controller_SetPhaseQueueing();
        DutyOps.QueueCasualMatch();
    }

    private void Controller_SetPhaseQueueing()
        => Plugin.Instance.Controller.Phase = AutoPhase.Queueing;

    private async Task RunStartupLifestream()
    {
        if (lifestreamCommand.Length == 0 || lifestreamCommand.Equals("none", StringComparison.OrdinalIgnoreCase))
            return;
        if (!LifestreamIPC.Instance.IsAvailable)
        {
            Warn("Lifestream command configured but Lifestream IPC is unavailable; skipping.");
            return;
        }

        Diag($"issuing lifestream command -> {lifestreamCommand}");
        LifestreamIPC.Instance.ExecuteCommand(lifestreamCommand);

        var started = await WaitUntilTimed(() =>
            Svc.Condition[ConditionFlag.Casting]
            || Svc.Condition[ConditionFlag.BetweenAreas]
            || Svc.Condition[ConditionFlag.BetweenAreas51]
            || LifestreamIPC.Instance.IsBusy(), 2_000, "lifestream-start");
        if (!started)
        {
            Diag("no lifestream activity detected after command; continuing");
            return;
        }

        await WaitUntilTimed(() =>
            !LifestreamIPC.Instance.IsBusy()
            && !Svc.Condition[ConditionFlag.BetweenAreas]
            && !Svc.Condition[ConditionFlag.BetweenAreas51]
            && Svc.Objects.LocalPlayer is not null, StartupZoneWaitMs, "lifestream-complete");
        Diag("lifestream zoning complete");
    }

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
        await NextFrame(5000);

        if (!Svc.Condition[ConditionFlag.InDutyQueue] && !DutyOps.IsQueued())
        {
            Diag("not queued -> queueing casual match roulette");
            DutyOps.QueueCasualMatch();
        }

        await NextFrame(500);
        return false;
    }
}
