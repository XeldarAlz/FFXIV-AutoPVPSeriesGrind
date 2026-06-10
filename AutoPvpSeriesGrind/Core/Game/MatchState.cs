using AutoPvpSeriesGrind.Core.Combat;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class MatchState
{
    public static bool InPvpArea()
        => CrystallineConflictMaps.Contains(Svc.ClientState.TerritoryType)
        || Svc.Condition[ConditionFlag.PvPDisplayActive];

    public static bool InDuty() => Svc.Condition[ConditionFlag.BoundByDuty];

    public static bool IsNormalConditions() => Svc.Condition[ConditionFlag.NormalConditions];

    public static bool LocalIsDead()
        => Svc.Condition[ConditionFlag.Unconscious]
        || (Svc.Objects.LocalPlayer is { } me && me.MaxHp > 0 && me.CurrentHp == 0);

    public static Vector3? PlayerPosition()
        => Svc.Objects.LocalPlayer?.Position;

    public static bool LocalIsCasting(uint actionId)
        => Svc.Objects.LocalPlayer is { } me && me.IsCasting(actionId);

    public static bool HasStatus(uint statusId)
        => Svc.Objects.LocalPlayer is { } localPlayer && HasStatus(localPlayer, statusId);

    private static bool HasStatus(IPlayerCharacter playerCharacter, uint statusId)
    {
        foreach (var status in playerCharacter.StatusList)
        {
            if (status is not null && status.StatusId == statusId)
            {
                return true;
            }
        }
        return false;
    }

    public static void SetTarget(ulong gameObjectId)
    {
        if (Svc.Targets.Target?.GameObjectId == gameObjectId)
        {
            return;
        }
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject.GameObjectId == gameObjectId)
            {
                Svc.Targets.Target = gameObject;
                return;
            }
        }
    }

    private static float SelfHpFraction()
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        if (localPlayer is null)
        {
            return 1f;
        }
        return HpFraction(localPlayer.CurrentHp, localPlayer.MaxHp);
    }

    private static PvpRole LocalRole()
        => RoleFromByte(Svc.Objects.LocalPlayer?.ClassJob.ValueNullable?.Role ?? 0);

    public static bool LocalIsMelee()
        => LocalRole() is PvpRole.Tank or PvpRole.Melee;

    private static Vector3? CrystalPosition()
    {
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject.Name.TextValue == ApsgConstants.CrystalName)
            {
                return gameObject.Position;
            }
        }
        return null;
    }

    public static bool IsEnemyPlayer(IGameObject gameObject)
    {
        try { return gameObject.IsHostile(); }
        catch { return false; }
    }

    public static Vector3? NearestSafeAnchor(uint territory, Vector3 from)
        => CrystallineConflictMaps.NearestSafeAnchor(territory, from);

    public static TeamBases? IdentifyBases(uint territory, Vector3 spawnPosition)
        => CrystallineConflictMaps.IdentifyBases(territory, spawnPosition);

    public static PvpSnapshot Capture()
    {
        var localPlayer = Svc.Objects.LocalPlayer;
        var self = localPlayer?.Position ?? Vector3.Zero;
        var selfId = localPlayer?.GameObjectId ?? 0;
        var selfTargetId = localPlayer?.TargetObjectId ?? 0;
        var objective = CrystalPosition();

        var players = ClassifyPlayers(localPlayer, self, selfId, selfTargetId);
        return AssembleSnapshot(self, selfId, objective, players);
    }

    private static ClassifiedPlayers ClassifyPlayers(IPlayerCharacter? localPlayer, Vector3 self, ulong selfId, ulong selfTargetId)
    {
        var enemies = new List<PvpActor>();
        var allies = new List<PvpActor>();
        var enemySum = Vector3.Zero;
        var allySum = Vector3.Zero;
        var focusCount = 0;
        PvpActor? currentTarget = null;

        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject is not IPlayerCharacter playerCharacter || playerCharacter.CurrentHp == 0)
            {
                continue;
            }
            if (localPlayer is not null && playerCharacter.Address == localPlayer.Address)
            {
                continue;
            }

            var actor = ToActor(playerCharacter, self);
            if (IsEnemyPlayer(playerCharacter))
            {
                enemies.Add(actor);
                enemySum += playerCharacter.Position;
                if (actor.TargetId == selfId)
                {
                    focusCount++;
                }
                if (selfTargetId != 0 && actor.Id == selfTargetId)
                {
                    currentTarget = actor;
                }
            }
            else
            {
                allies.Add(actor);
                allySum += playerCharacter.Position;
            }
        }

        return new ClassifiedPlayers(enemies, allies, enemySum, allySum, focusCount, currentTarget);
    }

    private static PvpSnapshot AssembleSnapshot(Vector3 self, ulong selfId, Vector3? objective, ClassifiedPlayers players)
    {
        return new PvpSnapshot
        {
            Self = self,
            SelfId = selfId,
            SelfHp = SelfHpFraction(),
            SelfRole = LocalRole(),
            Objective = objective,
            CurrentTarget = players.CurrentTarget,
            Enemies = players.Enemies,
            Allies = players.Allies,
            EnemyCentroid = players.Enemies.Count > 0 ? players.EnemySum / players.Enemies.Count : null,
            AllyCentroid = players.Allies.Count > 0 ? players.AllySum / players.Allies.Count : null,
            FocusCount = players.FocusCount,
        };
    }

    private static PvpActor ToActor(IPlayerCharacter playerCharacter, Vector3 self)
    {
        int roleByte = playerCharacter.ClassJob.ValueNullable?.Role ?? 0;
        return new PvpActor(
            Id: playerCharacter.GameObjectId,
            Position: playerCharacter.Position,
            Hp: HpFraction(playerCharacter.CurrentHp, playerCharacter.MaxHp),
            Role: RoleFromByte(roleByte),
            HasGuard: HasStatus(playerCharacter, ApsgConstants.StatusGuard),
            IsCasting: playerCharacter.IsCasting,
            TargetId: playerCharacter.TargetObjectId,
            DistanceToSelf: Vector3.Distance(self, playerCharacter.Position));
    }

    private static PvpRole RoleFromByte(int roleByte) => roleByte switch
    {
        ApsgConstants.JobRoles.Tank => PvpRole.Tank,
        ApsgConstants.JobRoles.MeleeDps => PvpRole.Melee,
        ApsgConstants.JobRoles.RangedDps => PvpRole.Ranged,
        ApsgConstants.JobRoles.Healer => PvpRole.Healer,
        _ => PvpRole.Unknown,
    };

    private static float HpFraction(uint currentHp, uint maxHp)
        => maxHp > 0 ? (float)currentHp / maxHp : 1f;

    private readonly record struct ClassifiedPlayers(
        List<PvpActor> Enemies,
        List<PvpActor> Allies,
        Vector3 EnemySum,
        Vector3 AllySum,
        int FocusCount,
        PvpActor? CurrentTarget);
}
