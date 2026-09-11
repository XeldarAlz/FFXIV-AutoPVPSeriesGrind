using FFXIVClientStructs.FFXIV.Client.Game;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class ActionOps
{
    public static void UseAction(uint actionId)
        => ActionManager.Instance()->UseAction(ActionType.Action, actionId);
}
