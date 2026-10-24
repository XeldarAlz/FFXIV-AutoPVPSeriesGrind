using ECommons.DalamudServices;
using LuminaAction = Lumina.Excel.Sheets.Action;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal enum LbCastShape : byte
{
    Unknown,
    SingleTarget,
    CircleAroundCaster,
    Cone,
    Line,
    CircleAroundTarget,
    GroundCircle,
    Donut,
    Cross,
}

internal readonly record struct LbTargetingProfile(
    uint ActionId,
    float Range,
    float EffectRange,
    LbCastShape Shape,
    bool TargetArea,
    bool RequiresEnemyTarget)
{
    private const float UnknownAoeFallbackYalms = 5f;

    public static LbTargetingProfile None => default;

    // A ground circle the game asks a placement for (Summon Bahamut, Mesotes) lands where it is placed, up to Range
    // away; one without it is centred on the caster like any other point-blank AoE.
    public bool CentersOnCaster => Shape is LbCastShape.CircleAroundCaster or LbCastShape.Donut
        || (Shape == LbCastShape.GroundCircle && !TargetArea);

    public float AoeRadius => EffectRange > 0f ? EffectRange : UnknownAoeFallbackYalms;

    public float CastRange(float scanRangeYalms) => Range > 0f ? MathF.Min(Range, scanRangeYalms) : scanRangeYalms;

    public static LbTargetingProfile FromAction(uint actionId)
    {
        if (!Svc.Data.GetExcelSheet<LuminaAction>().TryGetRow(actionId, out var row))
        {
            return None;
        }

        var range = row.Range < 0 ? 0f : row.Range;
        var shape = MapShape(row.CastType, range);
        // Southern Cross lands on a party member as readily as on an enemy, so only an action that cannot be cast on
        // yourself needs an enemy to aim at.
        var requiresEnemyTarget = row.CanTargetHostile && !row.CanTargetSelf;
        return new LbTargetingProfile(actionId, range, row.EffectRange, shape, row.TargetArea, requiresEnemyTarget);
    }

    // Cast type 2 is a circle around whatever the action is cast on: the caster when it has no range (Contradance),
    // the enemy when it has one (Zantetsuken, Deathflare, Encore of Light).
    private static LbCastShape MapShape(byte castType, float range) => castType switch
    {
        1 => LbCastShape.SingleTarget,
        2 => range > 0f ? LbCastShape.CircleAroundTarget : LbCastShape.CircleAroundCaster,
        3 => LbCastShape.Cone,
        4 => LbCastShape.Line,
        5 => LbCastShape.CircleAroundTarget,
        7 or 8 => LbCastShape.GroundCircle,
        10 or 13 => LbCastShape.Cross,
        11 => LbCastShape.Donut,
        12 => LbCastShape.Cone,
        _ => LbCastShape.Unknown,
    };
}
