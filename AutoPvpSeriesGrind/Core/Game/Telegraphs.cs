using AutoPvpSeriesGrind.Core.Combat;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using System.Numerics;
using GameAction = Lumina.Excel.Sheets.Action;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class Telegraphs
{
    private const byte SingleTargetCastType = 1;
    private const byte TargetedCircleCastType = 2;
    private const byte ConeCastType = 3;
    private const byte LineCastType = 4;
    private const float MinReactableCastSec = 0.5f;
    private const float PlacedZoneRadiusYalms = 8f;
    private const float MinVectorSq = 0.0001f;

    public static void CollectCast(IBattleChara caster, List<Hazard> hazards)
    {
        if (!caster.IsCasting)
        {
            return;
        }

        var castInfo = caster.CastInfo;
        if (castInfo.TotalCastTime - castInfo.CurrentCastTime < MinReactableCastSec)
        {
            return;
        }
        if (!Svc.Data.GetExcelSheet<GameAction>().TryGetRow(caster.CastActionId, out var action)
            || action.EffectRange == 0
            || action.CastType == SingleTargetCastType)
        {
            return;
        }

        var name = action.Name.ExtractText();
        var target = Svc.Objects.SearchById(caster.CastTargetObjectId);
        var facing = FacingOf(caster, target);
        switch (action.CastType)
        {
            case TargetedCircleCastType:
                var center = action.TargetArea ? (Vector3)castInfo.TargetLocation : target?.Position ?? caster.Position;
                hazards.Add(new Hazard(HazardShape.Circle, center, Vector3.UnitX, action.EffectRange, 0f, name));
                break;
            case ConeCastType:
                hazards.Add(new Hazard(HazardShape.Cone, caster.Position, facing, action.EffectRange, 0f, name));
                break;
            case LineCastType:
                hazards.Add(new Hazard(HazardShape.Line, caster.Position, facing, action.EffectRange, action.XAxisModifier / 2f, name));
                break;
            default:
                hazards.Add(new Hazard(HazardShape.Circle, caster.Position, Vector3.UnitX, action.EffectRange, 0f, name));
                break;
        }
    }

    public static void CollectPlacedZones(List<Hazard> hazards)
    {
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject.ObjectKind != ObjectKind.AreaObject)
            {
                continue;
            }
            if (Svc.Objects.SearchById(gameObject.OwnerId) is not IPlayerCharacter owner || !MatchState.IsEnemyPlayer(owner))
            {
                continue;
            }
            hazards.Add(new Hazard(HazardShape.Circle, gameObject.Position, Vector3.UnitX, PlacedZoneRadiusYalms, 0f, gameObject.Name.TextValue));
        }
    }

    private static Vector3 FacingOf(IBattleChara caster, IGameObject? target)
    {
        if (target is not null && target.GameObjectId != caster.GameObjectId)
        {
            var toward = target.Position - caster.Position;
            toward.Y = 0f;
            if (toward.LengthSquared() > MinVectorSq)
            {
                return Vector3.Normalize(toward);
            }
        }
        return new Vector3(MathF.Sin(caster.Rotation), 0f, MathF.Cos(caster.Rotation));
    }
}
