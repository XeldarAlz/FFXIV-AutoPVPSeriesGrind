namespace AutoPvpSeriesGrind.Core;

internal static class ApsgConstants
{
    public const string PrimaryCommand = "/apsg";
    public const string AliasCommand = "/pvpseries";

    public const string LogPrefix = "[APSG]";

    // ContentRoulette row id for Crystalline Conflict (Casual Match), the roulette the source script queues.
    public const byte CasualMatchRouletteId = 40;

    // Crystalline Conflict objective object; the bot fights on/around it.
    public const string CrystalName = "Tactical Crystal";

    // Player status ids read off the local player during a match.
    public const uint StatusSpawnProtection = 895; // active in the spawn pen before the gate opens
    public const uint StatusSprint = 1342;         // PvP sprint already up

    // Garo collaboration titles, flipped on start when the option is enabled (kept verbatim from the script).
    public const string GaroTitle1 = "barago";
    public const string GaroTitle2 = "garo";

    public const int SaveThrottleMs = 500;

    internal static class ThrottleKeys
    {
        public const string Save = "AutoPvpSeriesGrind.Save";
    }

    internal static class AddonNames
    {
        public const string MatchResults = "MKSRecord"; // PvP results screen
    }
}
