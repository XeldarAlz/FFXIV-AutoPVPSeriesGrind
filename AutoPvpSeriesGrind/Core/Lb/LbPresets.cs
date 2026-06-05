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
    public const int Version = 2;

    // Offensive: fire when an enemy is low enough that the LB's burst secures the kill.
    // Absolute threshold ≈ LB damage minus a buffer for in-flight heals/shields.
    private static LbRulePreset OffAbs(uint absolute)
        => new() { Mode = LbFireMode.Offensive, EnemyHpMode = ThresholdMode.Absolute, EnemyHpAbsolute = absolute };

    private static LbRulePreset OffPct(float percent)
        => new() { Mode = LbFireMode.Offensive, EnemyHpMode = ThresholdMode.Percent, EnemyHpPercent = percent };

    // Defensive: team heal/shield/mitigation — fire when allies (self included) are hurt AND enemies are present.
    private static LbRulePreset Def(float allyHp, int allies, float allyR, int enemies, float enemyR)
        => new() { Mode = LbFireMode.Defensive, AllyHpPercent = allyHp, AllyCountNear = allies, AllyRadiusYalms = allyR, EnemyCountNear = enemies, EnemyRadiusYalms = enemyR };

    // Utility: team buff or self-centered AoE setup/CC — fire in a teamfight. allies=1 means "self only",
    // i.e. the trigger reduces to "enough enemies clustered" for LBs that don't need teammates in range.
    private static LbRulePreset Util(int allies, float allyR, int enemies, float enemyR)
        => new() { Mode = LbFireMode.Utility, AllyCountNear = allies, AllyRadiusYalms = allyR, EnemyCountNear = enemies, EnemyRadiusYalms = enemyR };

    // Keyed by ClassJob RowId. Values are starting points derived from each LB's mechanic — tune to taste.
    public static readonly IReadOnlyDictionary<uint, LbRulePreset> Rules = new Dictionary<uint, LbRulePreset>
    {
        // ── Offensive (secure a kill) ──────────────────────────────────────────────
        [31] = OffAbs(33_500), // MCH Marksman's Spite — 40k single-target nuke
        [34] = OffAbs(20_000), // SAM Zantetsuken — 24k AoE, ignores Guard (Kuzushi = execute)
        [32] = OffAbs(18_000), // DRK Eventide — up to 24k line; scales with DRK's own HP, so conservative
        [20] = OffAbs(18_000), // MNK Meteodrive — 12k + 12k, removes Guard, roots
        [27] = OffAbs(16_500), // SMN Summon Bahamut — Megaflare 20k AoE
        [22] = OffAbs(16_000), // DRG Sky High → Sky Shatter — 16k AoE (32k within 5y)
        [24] = OffAbs(15_000), // WHM Afflatus Purgation — 18k line + stun + team regen (hybrid; see notes)
        [41] = OffAbs(12_500), // VPR World-Swallower — 15k AoE + Reawakened burst window
        [30] = OffPct(48f),    // NIN Seiton Tenchu — incapacitates foes below 50% HP

        // ── Defensive (team heal / shield / mitigation) ────────────────────────────
        [19] = Def(allyHp: 60f, allies: 2, allyR: 15f, enemies: 2, enemyR: 15f), // PLD Phalanx — self invuln + party 33% DR
        [40] = Def(allyHp: 65f, allies: 2, allyR: 12f, enemies: 2, enemyR: 15f), // SGE Mesotes — damage-negate barrier zone
        [42] = Def(allyHp: 70f, allies: 2, allyR: 15f, enemies: 2, enemyR: 15f), // PCT Chocobastion — -25% dmg-taken zone + knockback
        [28] = Def(allyHp: 70f, allies: 2, allyR: 20f, enemies: 1, enemyR: 25f), // SCH Seraphism — heal ramp + cleanse/barrier
        [35] = Def(allyHp: 70f, allies: 2, allyR: 15f, enemies: 2, enemyR: 20f), // RDM Southern Cross — 12k party heal + 12k AoE

        // ── Utility (teamfight buff / setup / CC) ──────────────────────────────────
        [33] = Util(allies: 2, allyR: 20f, enemies: 2, enemyR: 20f), // AST Celestial River — team +30% dmg/heal, enemies -30% dmg
        [23] = Util(allies: 2, allyR: 25f, enemies: 2, enemyR: 25f), // BRD Final Fantasia — +10% party damage (30y)
        [21] = Util(allies: 1, allyR: 20f, enemies: 2, enemyR: 12f), // WAR Primal Scream — cone Guard-strip + Inner Release
        [37] = Util(allies: 1, allyR: 20f, enemies: 2, enemyR: 8f),  // GNB Relentless Rush — PBAoE DoT + self mit + dmg-taken debuff
        [25] = Util(allies: 1, allyR: 20f, enemies: 2, enemyR: 20f), // BLM Soul Resonance — upgrades to Flare/Freeze AoE
        [38] = Util(allies: 1, allyR: 20f, enemies: 2, enemyR: 12f), // DNC Contradance — AoE charm (Seduced)
        [39] = Util(allies: 1, allyR: 20f, enemies: 2, enemyR: 10f), // RPR Tenebrae Lemurum — self burst + AoE Hysteria stun
    };

    public static string ToJson() => JsonConvert.SerializeObject(Rules);
}
