using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class ActionOps
{
    private const uint ReadyStatus = 0;
    private const uint InRangeAndInSight = 0;
    private const uint InRangeButNotFacing = 565;

    public static bool AnimationLocked => ActionManager.Instance()->AnimationLock > 0f;

    public static uint Adjusted(uint actionId) => ActionManager.Instance()->GetAdjustedActionId(actionId);

    public static bool IsReady(uint actionId)
        => ActionManager.Instance()->GetActionStatus(ActionType.Action, actionId) == ReadyStatus;

    public static bool IsReadyIgnoringRecast(uint actionId)
        => ActionManager.Instance()->GetActionStatus(ActionType.Action, actionId, checkRecastActive: false, checkCastingActive: false) == ReadyStatus;

    public static bool HasQueuedAction => ActionManager.Instance()->QueuedActionId != 0;

    public static void CancelCast() => UIState.Instance()->Hotbar.CancelCast();

    public static bool InRangeAndSight(uint actionId, IGameObject target)
        => ActionManager.GetActionInRangeOrLoS(actionId, Player.GameObject, (GameObject*)target.Address) is InRangeAndInSight or InRangeButNotFacing;

    public static float RecastRemainingSeconds(byte cooldownGroup)
    {
        var detail = ActionManager.Instance()->GetRecastGroupDetail(cooldownGroup - 1);
        if (detail == null || !detail->IsActive)
        {
            return 0f;
        }
        return MathF.Max(0f, detail->Total - detail->Elapsed);
    }

    public static uint CurrentCharges(uint actionId) => ActionManager.Instance()->GetCurrentCharges(actionId);

    public static void UseAction(uint actionId)
        => ActionManager.Instance()->UseAction(ActionType.Action, actionId);

    public static bool UseAction(uint actionId, ulong targetId)
        => ActionManager.Instance()->UseAction(ActionType.Action, actionId, targetId);

    public static bool UseActionAt(uint actionId, ulong selfId, Vector3 location)
        => ActionManager.Instance()->UseActionLocation(ActionType.Action, actionId, selfId, &location);
}
