using AutoPvpSeriesGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed class AutoAfterRun(AfterRunAction action) : AutoCommon
{
    private readonly AfterRunAction action = action;

    private const int ReadyWaitMs = 20_000;
    private const int YesnoWaitMs = 6_000;
    private const int PreCommandSettleMs = 800;
    private const int LifestreamStartMs = 4_000;
    private const int LifestreamCompleteMs = 180_000;

    protected override async Task Execute()
    {
        await WaitUntilTimed(IsSafeToFinish, ReadyWaitMs, "afterrun-ready");
        if (CancelToken.IsCancellationRequested) return;

        switch (action)
        {
            case AfterRunAction.ReturnToInn:
                await ReturnToInn();
                break;

            case AfterRunAction.Logout:
                Status = "Logging out";
                Diag("After-run: logging out.");
                await NextFrame(PreCommandSettleMs);
                Chat.ExecuteCommand(ApsgConstants.GameCommands.Logout);
                if (await WaitUntilTimed(SelectYesnoOpen, YesnoWaitMs, "logout-yesno"))
                {
                    ClickYes();
                    Diag("Logout confirmation accepted.");
                }
                else
                {
                    Warn($"Logout confirmation did not appear within {YesnoWaitMs / 1000}s; re-issuing /logout.");
                    await NextFrame(PreCommandSettleMs);
                    Chat.ExecuteCommand(ApsgConstants.GameCommands.Logout);
                    if (await WaitUntilTimed(SelectYesnoOpen, YesnoWaitMs, "logout-yesno-retry"))
                        ClickYes();
                    else
                        Warn("Logout confirmation still absent after retry; character may remain logged in.");
                }
                break;

            case AfterRunAction.CloseGame:
                Status = "Closing the game";
                Diag("After-run: closing the game (/xlkill).");
                await NextFrame(PreCommandSettleMs);
                Chat.ExecuteCommand(ApsgConstants.GameCommands.CloseGame);
                break;
        }
    }

    private async Task ReturnToInn()
    {
        Status = "Returning to the inn";
        if (!LifestreamIPC.Instance.IsAvailable)
        {
            Warn("Return to inn requested but Lifestream is not installed; staying put.");
            ApsgLog.ChatError("Install Lifestream to use \"Return to the inn\".");
            return;
        }

        Diag("After-run: returning to the inn via Lifestream.");
        ApsgLog.Chat("Run complete — retiring to the inn.");
        await NextFrame(PreCommandSettleMs);
        LifestreamIPC.Instance.ExecuteCommand(ApsgConstants.LifestreamCommands.ReturnToInn);

        var started = await WaitUntilTimed(() =>
            LifestreamIPC.Instance.IsBusy()
            || Svc.Condition[ConditionFlag.Casting]
            || Svc.Condition[ConditionFlag.BetweenAreas]
            || Svc.Condition[ConditionFlag.BetweenAreas51], LifestreamStartMs, "inn-start");
        if (!started)
        {
            Diag("No Lifestream activity after the inn command; nothing more to do.");
            return;
        }

        await WaitUntilTimed(() =>
            !LifestreamIPC.Instance.IsBusy()
            && !Svc.Condition[ConditionFlag.BetweenAreas]
            && !Svc.Condition[ConditionFlag.BetweenAreas51]
            && Svc.Objects.LocalPlayer is not null, LifestreamCompleteMs, "inn-complete");
        Diag("Return to inn complete.");
    }

    private static bool IsSafeToFinish()
        => Svc.Objects.LocalPlayer is not null
        && !Svc.Condition[ConditionFlag.InCombat]
        && !Svc.Condition[ConditionFlag.BoundByDuty]
        && !Svc.Condition[ConditionFlag.BetweenAreas]
        && !Svc.Condition[ConditionFlag.Casting];

    private static unsafe bool SelectYesnoOpen()
        => GenericHelpers.TryGetAddonByName<AtkUnitBase>(ApsgConstants.AddonNames.SelectYesno, out var a) && GenericHelpers.IsAddonReady(a);

    private static unsafe void ClickYes()
    {
        if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(ApsgConstants.AddonNames.SelectYesno, out var a) && GenericHelpers.IsAddonReady(a))
            new AddonMaster.SelectYesno((nint)a).Yes();
    }
}
