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
    // Per-territory vnav safe points (two spawn-side endpoints each). Each map ships under a base and
    // an "alt" territory id that share the same geometry, so both ids register the same anchors.
    public static readonly IReadOnlyDictionary<uint, SpawnAnchors> SafeAnchors = BuildSafeAnchors();

    private static Dictionary<uint, SpawnAnchors> BuildSafeAnchors()
    {
        var map = new Dictionary<uint, SpawnAnchors>();

        void Register(SpawnAnchors anchors, params uint[] territoryIds)
        {
            foreach (var id in territoryIds) map[id] = anchors;
        }

        Register(new(new(70.08521270752f, 4.0f, -9.7963066101074f), new(-72.121978759766f, 3.9999887943268f, 9.7666854858398f)),
            1032, 1058); // Palaistra
        Register(new(new(60.159770965576f, -1.5f, -20.096973419189f), new(-59.741413116455f, -1.5f, -20.130617141724f)),
            1033, 1059); // Volcanic Heart
        Register(new(new(-90.087173461914f, 6.2741222381592f, 78.478736877441f), new(89.641860961914f, 6.2917737960815f, -72.475570678711f)),
            1034, 1060); // Cloud Nine
        Register(new(new(59.628620147705f, -4.887580871582e-06f, 30.043525695801f), new(-59.981777191162f, 1.1920928955078e-07f, -30.034025192261f)),
            1116, 1117); // Clockwork Castletown
        Register(new(new(-103.6203994751f, 2.000935792923f, -50.288391113281f), new(102.09278869629f, 2.0002493858337f, 50.151763916016f)),
            1138, 1139); // Red Sands
        Register(new(new(187.177f, -2.000f, 99.600f), new(11.792f, -2.000f, 100.139f)),
            1293, 1294); // Bayside Battleground
        Register(new(new(24.983f, 1.001f, 117.666f), new(174.256f, 1.001f, 82.660f)),
            1357, 1358); // Archeia Harmonias

        return map;
    }

    private static readonly IReadOnlyDictionary<uint, uint[]> ObjectiveDataIds = new Dictionary<uint, uint[]>();

    public static bool InPvpArea()
        => SafeAnchors.ContainsKey(Svc.ClientState.TerritoryType)
        || Svc.Condition[ConditionFlag.PvPDisplayActive];

    public static uint LocalJobId()
        => Svc.Objects.LocalPlayer?.ClassJob.RowId ?? 0;

    public static Vector3? PlayerPosition()
        => Svc.Objects.LocalPlayer?.Position;

    public static bool LocalIsCasting(uint actionId)
        => Svc.Objects.LocalPlayer is { } me && me.IsCasting(actionId);

    public static bool HasStatus(uint statusId)
    {
        var me = Svc.Objects.LocalPlayer;
        if (me is null) return false;
        foreach (var s in me.StatusList)
            if (s is not null && s.StatusId == statusId) return true;
        return false;
    }

    public static float SelfHp01()
    {
        var me = Svc.Objects.LocalPlayer;
        if (me is null || me.MaxHp == 0) return 1f;
        return (float)me.CurrentHp / me.MaxHp;
    }

    public static bool LocalPrefersBackline()
    {
        var role = Svc.Objects.LocalPlayer?.ClassJob.ValueNullable?.Role ?? 0;
        return role is ApsgConstants.JobRoles.RangedDps or ApsgConstants.JobRoles.Healer;
    }

    public static bool LocalIsMelee()
        => (Svc.Objects.LocalPlayer?.ClassJob.ValueNullable?.Role ?? 0)
            is ApsgConstants.JobRoles.Tank or ApsgConstants.JobRoles.MeleeDps;

    public static Vector3? CrystalPosition()
    {
        var territory = Svc.ClientState.TerritoryType;
        var haveIds = ObjectiveDataIds.TryGetValue(territory, out var ids);
        foreach (var obj in Svc.Objects)
        {
            if (haveIds && obj is IEventObj && Array.IndexOf(ids!, obj.BaseId) >= 0)
                return obj.Position;
            if (obj.Name.TextValue == ApsgConstants.CrystalName)
                return obj.Position;
        }
        return null;
    }

    public static bool IsEnemyPlayer(IGameObject o)
    {
        try { return o.IsHostile(); }
        catch { return false; }
    }

    public static Vector3? NearestSafeAnchor(uint territory, Vector3 from)
        => SafeAnchors.TryGetValue(territory, out var anchors) ? anchors.Nearest(from) : null;

    public static PvpSnapshot Capture()
    {
        var me = Svc.Objects.LocalPlayer;
        var self = me?.Position ?? Vector3.Zero;
        var selfId = me?.GameObjectId ?? 0;
        var selfTargetId = me?.TargetObjectId ?? 0;
        var objective = CrystalPosition();

        var enemies = new List<PvpActor>();
        var allies = new List<PvpActor>();
        var enemySum = Vector3.Zero;
        var allySum = Vector3.Zero;
        var focus = 0;
        PvpActor? currentTarget = null;

        foreach (var obj in Svc.Objects)
        {
            if (obj is not IPlayerCharacter pc || pc.CurrentHp == 0) continue;
            if (me is not null && pc.Address == me.Address) continue;

            var actor = ToActor(pc, self, objective);
            if (IsEnemyPlayer(pc))
            {
                enemies.Add(actor);
                enemySum += pc.Position;
                if (actor.TargetId == selfId) focus++;
                if (selfTargetId != 0 && actor.Id == selfTargetId) currentTarget = actor;
            }
            else
            {
                allies.Add(actor);
                allySum += pc.Position;
            }
        }

        return new PvpSnapshot
        {
            Self = self,
            SelfId = selfId,
            SelfHp = SelfHp01(),
            SelfRole = RoleFromByte(me?.ClassJob.ValueNullable?.Role ?? 0),
            PrefersBackline = LocalPrefersBackline(),
            Objective = objective,
            CurrentTarget = currentTarget,
            Enemies = enemies,
            Allies = allies,
            EnemyCentroid = enemies.Count > 0 ? enemySum / enemies.Count : null,
            AllyCentroid = allies.Count > 0 ? allySum / allies.Count : null,
            FocusCount = focus,
            Territory = Svc.ClientState.TerritoryType,
        };
    }

    private static PvpActor ToActor(IPlayerCharacter pc, Vector3 self, Vector3? objective)
    {
        int roleByte = pc.ClassJob.ValueNullable?.Role ?? 0;
        return new PvpActor(
            Id: pc.GameObjectId,
            Position: pc.Position,
            Hp: pc.MaxHp > 0 ? (float)pc.CurrentHp / pc.MaxHp : 1f,
            CurrentHp: pc.CurrentHp,
            Role: RoleFromByte(roleByte),
            IsMelee: roleByte is ApsgConstants.JobRoles.Tank or ApsgConstants.JobRoles.MeleeDps,
            IsCasting: pc.IsCasting,
            TargetId: pc.TargetObjectId,
            DistanceToSelf: Vector3.Distance(self, pc.Position),
            DistanceToObjective: objective is { } o ? Vector3.Distance(o, pc.Position) : float.MaxValue);
    }

    private static CombatRole RoleFromByte(int role) => role switch
    {
        ApsgConstants.JobRoles.Tank => CombatRole.Tank,
        ApsgConstants.JobRoles.MeleeDps or ApsgConstants.JobRoles.RangedDps => CombatRole.DPS,
        ApsgConstants.JobRoles.Healer => CombatRole.Healer,
        _ => CombatRole.NonCombat,
    };
}
