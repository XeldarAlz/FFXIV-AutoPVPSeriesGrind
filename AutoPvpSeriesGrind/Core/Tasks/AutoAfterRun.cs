using AutoPvpSeriesGrind.Core.Ipc;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

// Runs once the grind hits its goal: the user's chosen "Then" action. Logout/CloseGame issue the matching
// chat command (driving the confirm dialog for logout); ReturnToInn defers to Lifestream's "inn" travel
// since this plugin has no navigation of its own.
public sealed class AutoAfterRun(AfterRunAction action) : AutoCommon
{
    private readonly AfterRunAction action = action;

    private const int ReadyWaitMs = 20_000;
    private const int YesnoWaitMs = 6_000;
    // Settle delay before issuing each chat command so it isn't eaten mid-transition.
    private const int PreCommandSettleMs = 800;
    private const int LifestreamStartMs = 4_000;
    private const int LifestreamCompleteMs = 180_000;

    protected override async Task Execute()
    {
        // Don't act mid-transition; wait for a clean grounded state first.
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
                Chat.ExecuteCommand("/logout");
                if (await WaitUntilTimed(SelectYesnoOpen, YesnoWaitMs, "logout-yesno"))
                {
                    ClickYes();
                    Diag("Logout confirmation accepted.");
                }
                else
                {
                    // The confirmation can fail to surface if the command was eaten (lag, a blocking
                    // addon); re-issue once before giving up so we don't silently stay logged in.
                    Warn($"Logout confirmation did not appear within {YesnoWaitMs / 1000}s; re-issuing /logout.");
                    await NextFrame(PreCommandSettleMs);
                    Chat.ExecuteCommand("/logout");
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
                Chat.ExecuteCommand("/xlkill");
                break;
        }
    }

    private async Task ReturnToInn()
    {
        Status = "Returning to the inn";
        if (!LifestreamIPC.Instance.IsAvailable)
        {
            Warn("Return to inn requested but Lifestream is not installed; staying put.");
            Svc.Chat.PrintError($"{ApsgConstants.LogPrefix} Install Lifestream to use \"Return to the inn\".");
            return;
        }

        Diag("After-run: returning to the inn via Lifestream.");
        Svc.Chat.Print($"{ApsgConstants.LogPrefix} Run complete — retiring to the inn.");
        await NextFrame(PreCommandSettleMs);
        LifestreamIPC.Instance.ExecuteCommand("inn");

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
