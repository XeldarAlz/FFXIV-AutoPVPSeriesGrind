using static AutoPvpSeriesGrind.Core.Rotation.Rule;
using static AutoPvpSeriesGrind.Core.Rotation.When;

namespace AutoPvpSeriesGrind.Core.Rotation;

// The PvP role action sits on one shared button that turns into whichever role action the player picked, so its
// rules are looked up by what that button currently shows. Thresholds follow RotationSolver Reborn's PvP defaults.
internal static class RoleActionRules
{
    public const uint Rampage = 43243;
    public const uint Rampart = 43244;
    public const uint FullSwing = 43245;
    public const uint Bloodbath = 43246;
    public const uint Swift = 43247;
    public const uint Smite = 43248;
    public const uint Dervish = 43249;
    public const uint Bravery = 43250;
    public const uint EagleEyeShot = 43251;
    public const uint Comet = 43252;
    public const uint Rust = 43254;
    public const uint Haelan = 43255;
    public const uint StoneskinII = 43256;
    public const uint Diabrosis = 43257;
    public const uint PhantomDart = 43291;

    private const float RampageRadiusYalms = 10f;
    private const float StoneskinRadiusYalms = 15f;
    private const float BuffEngageYalms = 25f;
    private const float MeleeReachYalms = 5f;

    private static readonly RotationRule[] Unknown = [Use(PvpActions.RoleAction, InCombat)];

    private static readonly Dictionary<uint, RotationRule[]> Rules = new()
    {
        [Rampage] = [UseAsOnSelf(PvpActions.RoleAction, Rampage, EnemiesWithin(RampageRadiusYalms))],
        [Rampart] = [UseAsOnSelf(PvpActions.RoleAction, Rampart, InCombat, SelfHpBelow(0.7f))],
        [FullSwing] = [UseAs(PvpActions.RoleAction, FullSwing)],
        [Bloodbath] = [UseAsOnSelf(PvpActions.RoleAction, Bloodbath, InCombat, SelfHpBelow(0.75f), EnemiesWithin(MeleeReachYalms))],
        [Swift] = [UseAsOnSelf(PvpActions.RoleAction, Swift, InCombat, TargetBeyond(MeleeReachYalms), TargetWithin(BuffEngageYalms))],
        [Smite] = [UseAsOnWeakest(PvpActions.RoleAction, Smite, EnemyInReachBelow(0.25f))],
        [Dervish] = [UseAsOnSelf(PvpActions.RoleAction, Dervish, InCombat, EnemiesWithin(BuffEngageYalms))],
        [Bravery] = [UseAsOnSelf(PvpActions.RoleAction, Bravery, InCombat, EnemiesWithin(BuffEngageYalms))],
        [EagleEyeShot] = [UseAs(PvpActions.RoleAction, EagleEyeShot)],
        [Comet] = [UseAs(PvpActions.RoleAction, Comet)],
        [Rust] = [UseAs(PvpActions.RoleAction, Rust)],
        [Haelan] = [UseAs(PvpActions.RoleAction, Haelan)],
        [StoneskinII] =
        [
            UseAsOnSelf(PvpActions.RoleAction, StoneskinII, InCombat, SelfHpBelow(0.8f)),
            UseAsOnSelf(PvpActions.RoleAction, StoneskinII, InCombat, AllyBelow(0.8f, StoneskinRadiusYalms)),
        ],
        [Diabrosis] = [UseAs(PvpActions.RoleAction, Diabrosis)],
        [PhantomDart] = [UseAs(PvpActions.RoleAction, PhantomDart)],
    };

    public static RotationRule[] For(uint shownOnButton) => Rules.TryGetValue(shownOnButton, out var rules) ? rules : Unknown;
}
