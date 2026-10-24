using AutoPvpSeriesGrind.Core.Game;
using Dalamud.Game.ClientState.Objects.Types;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal sealed class LbFirer(TargetSwapper swapper)
{
    private readonly TargetSwapper swapper = swapper;
    private readonly Dictionary<uint, uint> pendingActionBySlot = [];

    public long LastSubmitAtMs { get; private set; } = long.MinValue / 2;

    // Resubmitted on every ready tick, not throttled, so nothing else pressing buttons can overwrite the queued LB.
    // A used limit break turns into its follow-up on the same button (Deathflare, Encore of Light, Flare Star); pending
    // is kept per slot because the button has already changed by the time the sent action is seen to have gone off.
    public bool TryFire(uint jobId, IBattleChara target, out uint firedActionId)
    {
        firedActionId = 0;
        var slotIds = LbCatalog.ResolveActionIds(jobId);
        var targetId = (ulong)target.EntityId;
        for (var slotIndex = 0; slotIndex < slotIds.Length; slotIndex++)
        {
            var slotId = slotIds[slotIndex];
            if (pendingActionBySlot.TryGetValue(slotId, out var pendingActionId) && !ActionOps.IsReady(pendingActionId, targetId))
            {
                pendingActionBySlot.Remove(slotId);
                firedActionId = pendingActionId;
                return true;
            }

            var actionId = ActionOps.Adjusted(slotId);
            if (IsResolvedByEarlierSlot(slotIds, slotIndex, actionId) || !ActionOps.IsReady(actionId, targetId))
            {
                continue;
            }

            swapper.Swap(target);
            // Summon Bahamut and Mesotes want a placement: on the target for an offensive rule, on the player for a
            // defensive or utility one, which is what the decision hands over as the hard target in either case.
            var used = LbCatalog.ProfileFor(actionId).TargetArea
                ? ActionOps.UseActionAt(actionId, targetId, target.Position)
                : ActionOps.UseAction(actionId, targetId);
            if (used)
            {
                pendingActionBySlot[slotId] = actionId;
                LastSubmitAtMs = Environment.TickCount64;
            }
        }

        return false;
    }

    // A limit break can resolve to a follow-up the catalog also lists on its own; it is sent once so it counts once.
    private static bool IsResolvedByEarlierSlot(uint[] slotIds, int slotIndex, uint actionId)
    {
        for (var earlierIndex = 0; earlierIndex < slotIndex; earlierIndex++)
        {
            if (ActionOps.Adjusted(slotIds[earlierIndex]) == actionId)
            {
                return true;
            }
        }

        return false;
    }
}
