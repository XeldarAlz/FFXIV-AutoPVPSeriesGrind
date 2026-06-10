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
// PvpAutoLb.Core.LbRule exactly. Source is intentionally omitted; the receiver stamps it.
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

    private static class JobId
    {
        public const uint Paladin = 19;
        public const uint Monk = 20;
        public const uint Warrior = 21;
        public const uint Dragoon = 22;
        public const uint Bard = 23;
        public const uint WhiteMage = 24;
        public const uint BlackMage = 25;
        public const uint Summoner = 27;
        public const uint Scholar = 28;
        public const uint Ninja = 30;
        public const uint Machinist = 31;
        public const uint DarkKnight = 32;
        public const uint Astrologian = 33;
        public const uint Samurai = 34;
        public const uint RedMage = 35;
        public const uint Gunbreaker = 37;
        public const uint Dancer = 38;
        public const uint Reaper = 39;
        public const uint Sage = 40;
        public const uint Viper = 41;
        public const uint Pictomancer = 42;
    }

    // Offensive: fire when an enemy is low enough that the LB's burst secures the kill.
    // Absolute threshold ≈ LB damage minus a buffer for in-flight heals/shields.
    private static LbRulePreset OffensiveAbsolute(uint absolute)
        => new() { Mode = LbFireMode.Offensive, EnemyHpMode = ThresholdMode.Absolute, EnemyHpAbsolute = absolute };

    private static LbRulePreset OffensivePercent(float percent)
        => new() { Mode = LbFireMode.Offensive, EnemyHpMode = ThresholdMode.Percent, EnemyHpPercent = percent };

    // Defensive: team heal/shield/mitigation; fire when allies (self included) are hurt AND enemies are present.
    private static LbRulePreset Defensive(float allyHp, int allies, float allyRadius, int enemies, float enemyRadius)
        => new() { Mode = LbFireMode.Defensive, AllyHpPercent = allyHp, AllyCountNear = allies, AllyRadiusYalms = allyRadius, EnemyCountNear = enemies, EnemyRadiusYalms = enemyRadius };

    // Utility: team buff or self-centered AoE setup/CC; fire in a teamfight. allies=1 means "self only",
    // i.e. the trigger reduces to "enough enemies clustered" for LBs that don't need teammates in range.
    private static LbRulePreset Utility(int allies, float allyRadius, int enemies, float enemyRadius)
        => new() { Mode = LbFireMode.Utility, AllyCountNear = allies, AllyRadiusYalms = allyRadius, EnemyCountNear = enemies, EnemyRadiusYalms = enemyRadius };

    private static readonly IReadOnlyDictionary<uint, LbRulePreset> Rules = new Dictionary<uint, LbRulePreset>
    {
        [JobId.Machinist] = OffensiveAbsolute(33_500),
        [JobId.Samurai] = OffensiveAbsolute(20_000),
        [JobId.DarkKnight] = OffensiveAbsolute(18_000),
        [JobId.Monk] = OffensiveAbsolute(18_000),
        [JobId.Summoner] = OffensiveAbsolute(16_500),
        [JobId.Dragoon] = OffensiveAbsolute(16_000),
        [JobId.WhiteMage] = OffensiveAbsolute(15_000),
        [JobId.Viper] = OffensiveAbsolute(12_500),
        [JobId.Ninja] = OffensivePercent(48f),

        [JobId.Paladin] = Defensive(allyHp: 60f, allies: 2, allyRadius: 15f, enemies: 2, enemyRadius: 15f),
        [JobId.Sage] = Defensive(allyHp: 65f, allies: 2, allyRadius: 12f, enemies: 2, enemyRadius: 15f),
        [JobId.Pictomancer] = Defensive(allyHp: 70f, allies: 2, allyRadius: 15f, enemies: 2, enemyRadius: 15f),
        [JobId.Scholar] = Defensive(allyHp: 70f, allies: 2, allyRadius: 20f, enemies: 1, enemyRadius: 25f),
        [JobId.RedMage] = Defensive(allyHp: 70f, allies: 2, allyRadius: 15f, enemies: 2, enemyRadius: 20f),

        [JobId.Astrologian] = Utility(allies: 2, allyRadius: 20f, enemies: 2, enemyRadius: 20f),
        [JobId.Bard] = Utility(allies: 2, allyRadius: 25f, enemies: 2, enemyRadius: 25f),
        [JobId.Warrior] = Utility(allies: 1, allyRadius: 20f, enemies: 2, enemyRadius: 12f),
        [JobId.Gunbreaker] = Utility(allies: 1, allyRadius: 20f, enemies: 2, enemyRadius: 8f),
        [JobId.BlackMage] = Utility(allies: 1, allyRadius: 20f, enemies: 2, enemyRadius: 20f),
        [JobId.Dancer] = Utility(allies: 1, allyRadius: 20f, enemies: 2, enemyRadius: 12f),
        [JobId.Reaper] = Utility(allies: 1, allyRadius: 20f, enemies: 2, enemyRadius: 10f),
    };

    public static string ToJson() => JsonConvert.SerializeObject(Rules);
}
