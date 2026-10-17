using AutoPvpSeriesGrind.Core.Combat;
using Dalamud.Game.ClientState.Party;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class AlliancePositions
{
    // Roster-only positions are navigation hints, never combat/support targets.
    // Read fresh each snapshot; retain no positions across logout, death or territory changes.
    public static IReadOnlyList<PvpActor> ForRegrouping(IReadOnlyList<PvpActor> visible, Vector3 self, ulong selfId)
    {
        var result = new List<PvpActor>(visible);
        var seen = new HashSet<uint>(visible.Select(actor => (uint)actor.Id)) { (uint)selfId };
        for (var i = 0; i < Svc.Party.Length; i++)
        {
            try { Add(Svc.Party[i], self, seen, result); }
            catch { /* A roster slot can disappear during an update. */ }
        }
        if (Svc.Party.IsAlliance)
        {
            for (var i = 0; i < 24; i++)
            {
                try
                {
                    var address = Svc.Party.GetAllianceMemberAddress(i);
                    if (address != 0) Add(Svc.Party.CreateAllianceMemberReference(address), self, seen, result);
                }
                catch { /* Unavailable alliance slots do not replace visible actors. */ }
            }
        }
        return result;
    }

    private static void Add(IPartyMember? member, Vector3 self, HashSet<uint> seen, List<PvpActor> result)
    {
        if (member is null || member.CurrentHP == 0 || member.MaxHP == 0
            || member.EntityId is 0 or 0xE0000000 || seen.Contains(member.EntityId)) return;
        // Visible actors are already represented, including their current statuses.
        if (member.GameObject is not null) return;
        var position = member.Position;
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z)
            || position == Vector3.Zero) return;
        var distance = Vector3.Distance(self, position);
        if (distance > 180f) return;
        seen.Add(member.EntityId);
        result.Add(new PvpActor(member.EntityId, position, (float)member.CurrentHP / member.MaxHP,
            PvpRole.Unknown, false, false, false, 0, distance, CanAttack: false));
    }
}
