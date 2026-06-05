using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Debug;

internal static unsafe class TargetDumper
{
    public static void DumpObjects()
    {
        var me = Svc.Objects.LocalPlayer;
        if (me is null)
        {
            ApsgLog.Chat("No local player; enter a match first.");
            return;
        }

        var self = me.Position;
        var rows = Svc.Objects
            .Where(o => o.ObjectKind is Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj
                or Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc)
            .Select(o => (o, dist: Vector3.Distance(self, o.Position)))
            .OrderBy(t => t.dist)
            .Take(25)
            .ToList();

        ApsgLog.Chat($"Territory {Svc.ClientState.TerritoryType} — {rows.Count} nearby event/battle objects (nearest first):");
        foreach (var (o, dist) in rows)
            Svc.Chat.Print($"  [{o.ObjectKind}] BaseId={o.BaseId} \"{o.Name.TextValue}\" d={dist:F1}");
    }

    public static void Dump()
    {
        var territoryId = Svc.ClientState.TerritoryType;
        var territoryName = Svc.Data.GetExcelSheet<TerritoryType>()
            ?.GetRowOrDefault(territoryId)
            ?.PlaceName.Value.Name.ToString() ?? "?";

        ApsgLog.Chat($"Territory: {territoryId} ({territoryName})");

        var target = TargetSystem.Instance()->Target;
        if (target == null)
        {
            ApsgLog.Chat("No target. Click an NPC or FATE marker first, then re-run /apsg target.");
            return;
        }

        var baseId = target->BaseId;
        var name = target->NameString;
        var residentName = Svc.Data.GetExcelSheet<ENpcResident>()
            ?.GetRowOrDefault(baseId)?.Singular.ToString() ?? name;

        ApsgLog.Chat($"Target: BaseId={baseId}  Name=\"{residentName}\"");
        ApsgLog.Info($"TargetDumper: territory={territoryId} BaseId={baseId} name='{residentName}'");
    }
}
