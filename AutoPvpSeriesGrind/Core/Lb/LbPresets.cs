using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AutoPvpSeriesGrind.Core.Lb;

public enum LbFireMode
{
    Offensive,
    Defensive,
    Utility,
}

public enum ThresholdMode
{
    Percent,
    Absolute,
}

// Wire DTO pushed to PvpAutoLb over IPC. Property names and string-enum values must match
// PvpAutoLb.Core.LbRule exactly. Source is intentionally omitted — the receiver stamps it.
public sealed class LbRulePreset
{
    [JsonConverter(typeof(StringEnumConverter))] public LbFireMode Mode { get; set; } = LbFireMode.Offensive;

    [JsonConverter(typeof(StringEnumConverter))] public ThresholdMode EnemyHpMode { get; set; } = ThresholdMode.Percent;
    public float EnemyHpPercent { get; set; } = 30f;
    public uint EnemyHpAbsolute { get; set; } = 7000;
    public int MinEnemiesInAoe { get; set; } = 1;

    public float AllyHpPercent { get; set; } = 50f;
    public int AllyCountNear { get; set; } = 2;
    public float AllyRadiusYalms { get; set; } = 15f;
    public int EnemyCountNear { get; set; } = 1;
    public float EnemyRadiusYalms { get; set; } = 20f;
}

public static class LbPresets
{
    // Bump whenever the table changes so already-configured clients receive the update.
    public const int Version = 1;

    private static LbRulePreset Offensive(float pct = 30f)
        => new() { Mode = LbFireMode.Offensive, EnemyHpPercent = pct };

    private static LbRulePreset Defensive(float allyHp = 50f, int allies = 2, float allyR = 15f, int enemies = 1, float enemyR = 20f)
        => new() { Mode = LbFireMode.Defensive, AllyHpPercent = allyHp, AllyCountNear = allies, AllyRadiusYalms = allyR, EnemyCountNear = enemies, EnemyRadiusYalms = enemyR };

    private static LbRulePreset Utility(int allies = 2, float allyR = 15f, int enemies = 2, float enemyR = 20f)
        => new() { Mode = LbFireMode.Utility, AllyCountNear = allies, AllyRadiusYalms = allyR, EnemyCountNear = enemies, EnemyRadiusYalms = enemyR };

    // Keyed by ClassJob RowId. Seed values — tune per class.
    public static readonly IReadOnlyDictionary<uint, LbRulePreset> Rules = new Dictionary<uint, LbRulePreset>
    {
        // Offensive — fire on a low-HP enemy.
        [20] = Offensive(), // MNK
        [22] = Offensive(), // DRG
        [24] = Offensive(), // WHM  (healer — likely Defensive; reclassify)
        [27] = Offensive(), // SMN
        [28] = Offensive(), // SCH  (healer — likely Defensive; reclassify)
        [30] = Offensive(), // NIN
        [31] = Offensive(), // MCH
        [32] = Offensive(), // DRK
        [34] = Offensive(), // SAM
        [35] = Offensive(), // RDM
        [37] = Offensive(), // GNB
        [41] = Offensive(), // VPR

        // Defensive — heals/shields; fire when the team is pressured.
        [19] = Defensive(), // PLD — Phalanx
        [33] = Defensive(), // AST — Celestial River
        [38] = Defensive(), // DNC — Contradance
        [40] = Defensive(), // SGE — Mesotes
        [42] = Defensive(), // PCT — Advent of Chocobastion

        // Utility — buffs/setup; fire in a teamfight.
        [21] = Utility(),   // WAR — Primal Scream
        [23] = Utility(),   // BRD — Final Fantasia
        [25] = Utility(),   // BLM — Soul Resonance
        [39] = Utility(),   // RPR — Tenebrae Lemurum
    };

    public static string ToJson() => JsonConvert.SerializeObject(Rules);
}
