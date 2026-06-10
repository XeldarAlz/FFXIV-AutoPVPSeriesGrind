using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel.Sheets;
using System.Numerics;
// FFXIVClientStructs.FFXIV.Client.Game.Object also defines an ObjectKind; alias to keep Dalamud's unambiguous.
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace AutoPvpSeriesGrind.Core.Debug;

internal static unsafe class TargetDumper
{
    private const int MaxNearbyObjectRows = 25;

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
            .Where(gameObject => gameObject.ObjectKind is ObjectKind.EventObj or ObjectKind.BattleNpc)
            .Select(gameObject => (gameObject, distance: Vector3.Distance(self, gameObject.Position)))
            .OrderBy(row => row.distance)
            .Take(MaxNearbyObjectRows)
            .ToList();

        ApsgLog.Chat($"Territory {Svc.ClientState.TerritoryType}: {rows.Count} nearby event/battle objects (nearest first):");
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var (gameObject, distance) = rows[rowIndex];
            Svc.Chat.Print($"  [{gameObject.ObjectKind}] BaseId={gameObject.BaseId} \"{gameObject.Name.TextValue}\" d={distance:F1}");
        }
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
