using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class AutoAfterRun(AfterRunAction action) : AutoCommon
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
                await LogoutWithConfirmation();
                break;

            case AfterRunAction.CloseGame:
                Status = "Closing the game";
                LogDiagnostic("After-run: closing the game (/xlkill).");
                await NextFrame(PreCommandSettleMs);
                Chat.ExecuteCommand(ApsgConstants.GameCommands.CloseGame);
                break;
        }
    }

    private async Task LogoutWithConfirmation()
    {
        Status = "Logging out";
        LogDiagnostic("After-run: logging out.");
        if (await IssueLogoutAndAcceptYesno("logout-yesno"))
        {
            LogDiagnostic("Logout confirmation accepted.");
            return;
        }

        Warn($"Logout confirmation did not appear within {YesnoWaitMs / 1000}s; re-issuing /logout.");
        if (!await IssueLogoutAndAcceptYesno("logout-yesno-retry"))
        {
            Warn("Logout confirmation still absent after retry; character may remain logged in.");
        }
    }

    private async Task<bool> IssueLogoutAndAcceptYesno(string waitScope)
    {
        await NextFrame(PreCommandSettleMs);
        Chat.ExecuteCommand(ApsgConstants.GameCommands.Logout);
        if (!await WaitUntilTimed(SelectYesnoOpen, YesnoWaitMs, waitScope))
        {
            return false;
        }

        ClickYes();
        return true;
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

        LogDiagnostic("After-run: returning to the inn via Lifestream.");
        ApsgLog.Chat("Run complete — retiring to the inn.");
        await NextFrame(PreCommandSettleMs);
        LifestreamIPC.Instance.ExecuteCommand(ApsgConstants.LifestreamCommands.ReturnToInn);

        var started = await WaitUntilTimed(() =>
            LifestreamIPC.Instance.IsBusy()
            || Svc.Condition[ConditionFlag.Casting]
            || IsTransitioning(), LifestreamStartMs, "inn-start");
        if (!started)
        {
            LogDiagnostic("No Lifestream activity after the inn command; nothing more to do.");
            return;
        }

        await WaitUntilTimed(() =>
            !LifestreamIPC.Instance.IsBusy()
            && !IsTransitioning()
            && Svc.Objects.LocalPlayer is not null, LifestreamCompleteMs, "inn-complete");
        LogDiagnostic("Return to inn complete.");
    }

    private static bool IsTransitioning()
        => Svc.Condition[ConditionFlag.BetweenAreas]
        || Svc.Condition[ConditionFlag.BetweenAreas51];

    private static bool IsSafeToFinish()
        => Svc.Objects.LocalPlayer is not null
        && !Svc.Condition[ConditionFlag.InCombat]
        && !Svc.Condition[ConditionFlag.BoundByDuty]
        && !Svc.Condition[ConditionFlag.BetweenAreas]
        && !Svc.Condition[ConditionFlag.Casting];

    private static bool SelectYesnoOpen() => AddonProbe.IsReady(ApsgConstants.AddonNames.SelectYesno);

    private static unsafe void ClickYes()
    {
        if (AddonProbe.TryGetReady(ApsgConstants.AddonNames.SelectYesno, out var addon))
        {
            new AddonMaster.SelectYesno((nint)addon).Yes();
        }
    }
}
