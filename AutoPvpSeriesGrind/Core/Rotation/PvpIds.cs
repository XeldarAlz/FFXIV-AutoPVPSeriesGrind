namespace AutoPvpSeriesGrind.Core.Rotation;

internal static class PvpActions
{
    public const uint Guard = 29054;
    public const uint StandardIssueElixir = 29055;
    public const uint Purify = 29056;
    public const uint Sprint = 29057;
    public const uint Recuperate = 29711;

    public static readonly uint[] Shared = [Guard, StandardIssueElixir, Purify, Sprint, Recuperate];
}

internal static class PvpStatuses
{
    public const uint SpawnProtection = 895;
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

    public static readonly uint[] PurifyClears = [Stun, Heavy, Bind, Silence, DeepFreeze, MiracleOfNature];
    public static readonly uint[] GuardForbiddenBy = [InnerRelease, UndeadRedemption];
}
