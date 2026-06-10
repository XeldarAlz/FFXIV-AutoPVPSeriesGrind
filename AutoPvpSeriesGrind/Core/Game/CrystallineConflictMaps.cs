using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal static class CrystallineConflictMaps
{
    // Per-territory vnav safe points (two spawn-side endpoints each). Each map ships under a base and
    // an "alt" territory id that share the same geometry, so both ids register the same anchors.
    private static readonly IReadOnlyDictionary<uint, SpawnAnchors> SafeAnchors = BuildSafeAnchors();

    public static bool Contains(uint territoryId) => SafeAnchors.ContainsKey(territoryId);

    public static Vector3? NearestSafeAnchor(uint territory, Vector3 from)
        => SafeAnchors.TryGetValue(territory, out var anchors) ? anchors.Nearest(from) : null;

    private static Dictionary<uint, SpawnAnchors> BuildSafeAnchors()
    {
        var map = new Dictionary<uint, SpawnAnchors>();

        void Register(SpawnAnchors anchors, params uint[] territoryIds)
        {
            for (var territoryIndex = 0; territoryIndex < territoryIds.Length; territoryIndex++)
            {
                map[territoryIds[territoryIndex]] = anchors;
            }
        }

        var palaistra = new SpawnAnchors(
            new(70.08521270752f, 4.0f, -9.7963066101074f), new(-72.121978759766f, 3.9999887943268f, 9.7666854858398f));
        var volcanicHeart = new SpawnAnchors(
            new(60.159770965576f, -1.5f, -20.096973419189f), new(-59.741413116455f, -1.5f, -20.130617141724f));
        var cloudNine = new SpawnAnchors(
            new(-90.087173461914f, 6.2741222381592f, 78.478736877441f), new(89.641860961914f, 6.2917737960815f, -72.475570678711f));
        var clockworkCastletown = new SpawnAnchors(
            new(59.628620147705f, -4.887580871582e-06f, 30.043525695801f), new(-59.981777191162f, 1.1920928955078e-07f, -30.034025192261f));
        var redSands = new SpawnAnchors(
            new(-103.6203994751f, 2.000935792923f, -50.288391113281f), new(102.09278869629f, 2.0002493858337f, 50.151763916016f));
        var baysideBattleground = new SpawnAnchors(
            new(187.177f, -2.000f, 99.600f), new(11.792f, -2.000f, 100.139f));
        var archeiaHarmonias = new SpawnAnchors(
            new(24.983f, 1.001f, 117.666f), new(174.256f, 1.001f, 82.660f));

        Register(palaistra, 1032, 1058);
        Register(volcanicHeart, 1033, 1059);
        Register(cloudNine, 1034, 1060);
        Register(clockworkCastletown, 1116, 1117);
        Register(redSands, 1138, 1139);
        Register(baysideBattleground, 1293, 1294);
        Register(archeiaHarmonias, 1357, 1358);

        return map;
    }
}
