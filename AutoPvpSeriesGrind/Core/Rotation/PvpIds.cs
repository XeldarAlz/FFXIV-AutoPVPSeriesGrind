namespace AutoPvpSeriesGrind.Core.Rotation;

internal static class PvpActions
{
    public const uint Guard = 29054;
    public const uint StandardIssueElixir = 29055;
    public const uint Purify = 29056;
    public const uint Sprint = 29057;
    public const uint Recuperate = 29711;

    public static readonly uint[] Shared = [Guard, StandardIssueElixir, Purify, Sprint, Recuperate];

    public static readonly uint[] WorthUsingIntoGuard =
    [
        29490, 29503, 29405, 41457, 41458, 41488, 41489, 41490, 29716,
        39174, 39175, 39176, 39177, 39178, 39179, 39180, 39181, 39182,
    ];
}

internal static class PvpStatuses
{
    public const uint SpawnProtection = 895;
    public const uint HallowedGround = 1302;
    public const uint InnerRelease = 1303;
    public const uint Sprint = 1342;
    public const uint Stun = 1343;
    public const uint Heavy = 1344;
    public const uint Bind = 1345;
    public const uint Silence = 1347;
    public const uint UndeadRedemption = 3039;
    public const uint Guard = 3054;
    public const uint MiracleOfNature = 3085;
    public const uint DeepFreeze = 3219;
    public const uint Slipping = 3227;
    public const uint WeakenedGuard = 3673;
    public const uint Lethargy = 4333;

    public static readonly uint[] PurifyClears = [Stun, Heavy, Bind, Silence, DeepFreeze, MiracleOfNature];
    public static readonly uint[] GuardForbiddenBy = [InnerRelease, UndeadRedemption];
    public static readonly uint[] DamageImmunities = [Guard, WeakenedGuard, HallowedGround, UndeadRedemption];
}
