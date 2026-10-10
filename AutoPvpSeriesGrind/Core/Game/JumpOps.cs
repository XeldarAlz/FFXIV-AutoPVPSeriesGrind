using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class JumpOps
{
    public static bool TryJump()
    {
        if (Svc.Condition[ConditionFlag.Jumping] || Svc.Condition[ConditionFlag.Jumping61] || MatchState.LocalIsCasting())
        {
            return false;
        }

        ActionOps.UseGeneralAction(GeneralActions.Jump);
        return true;
    }
}
