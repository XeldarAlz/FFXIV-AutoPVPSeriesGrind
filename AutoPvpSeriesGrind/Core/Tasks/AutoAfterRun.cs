using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Localization;
using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class AutoAfterRun(AfterRunAction action, string commandBlock) : AutoCommon
{
    private readonly AfterRunAction action = action;
    private readonly string commandBlock = commandBlock;

    private const int ReadyWaitMs = 20_000;
    private const int YesnoWaitMs = 6_000;
    private const int PreCommandSettleMs = 800;
    private const int LifestreamStartMs = 4_000;
    private const int LifestreamCompleteMs = 180_000;

    // Some plugin commands misbehave when several arrive in the same frame, and a logout must not cut the last one off.
    private const int ChatCommandSpacingMs = 500;

    protected override async Task Execute()
    {
        await WaitUntilTimed(IsFreeToAct, ReadyWaitMs, "afterrun-ready");
        if (CancelToken.IsCancellationRequested) return;

        await RunChatCommands();
        if (CancelToken.IsCancellationRequested)
        {
            return;
        }

        switch (action)
        {
            case AfterRunAction.ReturnToInn:
                await ReturnToInn();
                break;

            case AfterRunAction.Logout:
                await LogoutWithConfirmation();
                break;

            case AfterRunAction.CloseGame:
                Status = Loc.T(L.Status.ClosingGame);
                LogDiagnostic("After-run: closing the game (/xlkill).");
                await DelayMs(PreCommandSettleMs);
                Chat.ExecuteCommand(ApsgConstants.GameCommands.CloseGame);
                break;
        }
    }

    private async Task RunChatCommands()
    {
        var lines = ChatCommandBlock.Lines(commandBlock);
        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }

            var line = lines[lineIndex];
            if (!ChatCommandBlock.IsCommand(line))
            {
                Warn($"After-run line skipped, commands must start with '/': {line}");
                continue;
            }

            Status = Loc.T(L.Status.RunningCommands);
            SendChatCommand(line);
            await DelayMs(ChatCommandSpacingMs);
        }
    }

    private void SendChatCommand(string command)
    {
        try
        {
            LogDiagnostic($"After-run command: {command}");
            Chat.ExecuteCommand(command);
        }
        catch (Exception exception)
        {
            RunLog.Error($"After-run command '{command}' failed: {exception.Message}");
        }
    }

    private async Task LogoutWithConfirmation()
    {
        Status = Loc.T(L.Status.LoggingOut);
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
        await DelayMs(PreCommandSettleMs);
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
        Status = Loc.T(L.Status.ReturningToInn);
        if (!LifestreamIpc.Instance.IsAvailable)
        {
            Warn("Return to inn requested but Lifestream is not installed; staying put.");
            ApsgChat.PrintError(Loc.T(L.Notice.LifestreamMissing));
            return;
        }

        LogDiagnostic("After-run: returning to the inn via Lifestream.");
        ApsgChat.Print(Loc.T(L.Notice.RetiringToInn));
        await DelayMs(PreCommandSettleMs);
        LifestreamIpc.Instance.ExecuteCommand(ApsgConstants.LifestreamCommands.ReturnToInn);

        var started = await WaitUntilTimed(() =>
            LifestreamIpc.Instance.IsBusy()
            || Svc.Condition[ConditionFlag.Casting]
            || IsTransitioning(), LifestreamStartMs, "inn-start");
        if (!started)
        {
            LogDiagnostic("No Lifestream activity after the inn command; nothing more to do.");
            return;
        }

        await WaitUntilTimed(() =>
            !LifestreamIpc.Instance.IsBusy()
            && !IsTransitioning()
            && Svc.Objects.LocalPlayer is not null, LifestreamCompleteMs, "inn-complete");
        LogDiagnostic("Return to inn complete.");
    }

    private static bool IsTransitioning()
        => Svc.Condition[ConditionFlag.BetweenAreas]
        || Svc.Condition[ConditionFlag.BetweenAreas51];

    private static bool SelectYesnoOpen() => AddonProbe.IsReady(ApsgConstants.AddonNames.SelectYesno);

    private static unsafe void ClickYes()
    {
        if (AddonProbe.TryGetReady(ApsgConstants.AddonNames.SelectYesno, out var addon))
        {
            new AddonMaster.SelectYesno((nint)addon).Yes();
        }
    }
}
