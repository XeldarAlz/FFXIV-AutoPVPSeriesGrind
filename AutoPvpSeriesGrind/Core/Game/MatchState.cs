using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

// Read-only snapshot of the current PvP match: where we are, where the objective crystal is, whether it's
// being contested, and our own status. The movement/anchor data is ported verbatim from the source script.
internal static class MatchState
{
    // vnav safety-anchor endpoints per Crystalline Conflict map (two spawn-side points each). Used to walk
    // out of the spawn pen toward whichever side we landed on. Kept verbatim from the source script.
    public static readonly IReadOnlyDictionary<uint, float[]> SafeAnchors = new Dictionary<uint, float[]>
    {
        [1032] = [70.08521270752f, 4.0f, -9.7963066101074f, -72.121978759766f, 3.9999887943268f, 9.7666854858398f],   // Palaistra
        [1058] = [70.08521270752f, 4.0f, -9.7963066101074f, -72.121978759766f, 3.9999887943268f, 9.7666854858398f],   // Palaistra (alt)
        [1033] = [60.159770965576f, -1.5f, -20.096973419189f, -59.741413116455f, -1.5f, -20.130617141724f],           // Volcanic Heart
        [1059] = [60.159770965576f, -1.5f, -20.096973419189f, -59.741413116455f, -1.5f, -20.130617141724f],           // Volcanic Heart (alt)
        [1034] = [-90.087173461914f, 6.2741222381592f, 78.478736877441f, 89.641860961914f, 6.2917737960815f, -72.475570678711f], // Cloud Nine
        [1060] = [-90.087173461914f, 6.2741222381592f, 78.478736877441f, 89.641860961914f, 6.2917737960815f, -72.475570678711f], // Cloud Nine (alt)
        [1116] = [59.628620147705f, -4.887580871582e-06f, 30.043525695801f, -59.981777191162f, 1.1920928955078e-07f, -30.034025192261f], // Clockwork Castletown
        [1117] = [59.628620147705f, -4.887580871582e-06f, 30.043525695801f, -59.981777191162f, 1.1920928955078e-07f, -30.034025192261f], // Clockwork Castletown (alt)
        [1138] = [-103.6203994751f, 2.000935792923f, -50.288391113281f, 102.09278869629f, 2.0002493858337f, 50.151763916016f],   // Red Sands
        [1139] = [-103.6203994751f, 2.000935792923f, -50.288391113281f, 102.09278869629f, 2.0002493858337f, 50.151763916016f],   // Red Sands (alt)
        [1293] = [187.177f, -2.000f, 99.600f, 11.792f, -2.000f, 100.139f],   // Bayside Battleground
        [1294] = [187.177f, -2.000f, 99.600f, 11.792f, -2.000f, 100.139f],   // Bayside Battleground (alt)
        [1357] = [24.983f, 1.001f, 117.666f, 174.256f, 1.001f, 82.660f],     // Archeia Harmonias
        [1358] = [24.983f, 1.001f, 117.666f, 174.256f, 1.001f, 82.660f],     // Archeia Harmonias (alt)
    };

    public static bool InPvpArea()
        => SafeAnchors.ContainsKey(Svc.ClientState.TerritoryType)
        || Svc.Condition[ConditionFlag.PvPDisplayActive];

    public static uint LocalJobId()
        => Svc.Objects.LocalPlayer?.ClassJob.RowId ?? 0;

    public static Vector3? PlayerPosition()
        => Svc.Objects.LocalPlayer?.Position;

    public static bool HasStatus(uint statusId)
    {
        var me = Svc.Objects.LocalPlayer;
        if (me is null) return false;
        foreach (var s in me.StatusList)
            if (s is not null && s.StatusId == statusId) return true;
        return false;
    }

    public static Vector3? CrystalPosition()
    {
        foreach (var obj in Svc.Objects)
            if (obj.Name.TextValue == ApsgConstants.CrystalName)
                return obj.Position;
        return null;
    }

    // The source script holds position only when both the crystal is close AND an enemy is on it. Reading
    // the enemy party-list addon was fragile, so we instead count other players standing on the point: if
    // the objective is contested we stay and fight rather than re-pathing. Behaviour-equivalent for the grind.
    public static bool CrystalContested(Vector3 crystal, float radius)
    {
        foreach (var obj in Svc.Objects)
        {
            if (obj is not IPlayerCharacter pc) continue;
            if (pc.Address == Svc.Objects.LocalPlayer?.Address) continue;
            if (Vector3.Distance(pc.Position, crystal) < radius) return true;
        }
        return false;
    }
}
