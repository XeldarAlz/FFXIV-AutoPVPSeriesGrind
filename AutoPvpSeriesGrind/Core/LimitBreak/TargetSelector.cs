using AutoPvpSeriesGrind.Core.Game;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

// Both scans fill a list the caller keeps, sorted by HP ascending, so the per-tick scan allocates nothing.
internal static class TargetSelector
{
    public static void ScanHostiles(IPlayerCharacter self, float rangeYalms, List<IBattleChara> result)
    {
        result.Clear();
        var rangeSquared = rangeYalms * rangeYalms;
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject is not IBattleChara character || character.GameObjectId == self.GameObjectId
                || character.IsDead || !character.IsTargetable || !MatchState.IsEnemyPlayer(character)
                || !WithinRange(character.Position, self.Position, rangeSquared))
            {
                continue;
            }

            InsertByHp(result, character);
        }
    }

    public static void ScanAllies(IPlayerCharacter self, float rangeYalms, List<IBattleChara> result)
    {
        result.Clear();
        InsertByHp(result, self);
        var rangeSquared = rangeYalms * rangeYalms;
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject is not IPlayerCharacter player || player.GameObjectId == self.GameObjectId
                || player.IsDead || !player.IsTargetable || MatchState.IsEnemyPlayer(player)
                || !WithinRange(player.Position, self.Position, rangeSquared))
            {
                continue;
            }

            InsertByHp(result, player);
        }
    }

    private static bool WithinRange(Vector3 position, Vector3 origin, float rangeSquared)
    {
        var deltaX = position.X - origin.X;
        var deltaZ = position.Z - origin.Z;
        return deltaX * deltaX + deltaZ * deltaZ <= rangeSquared;
    }

    private static void InsertByHp(List<IBattleChara> list, IBattleChara character)
    {
        var insertIndex = list.Count;
        list.Add(character);
        while (insertIndex > 0 && list[insertIndex - 1].CurrentHp > character.CurrentHp)
        {
            list[insertIndex] = list[insertIndex - 1];
            insertIndex--;
        }

        list[insertIndex] = character;
    }
}
