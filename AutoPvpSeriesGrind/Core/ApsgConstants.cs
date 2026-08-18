using System.Numerics;

namespace AutoPvpSeriesGrind.Core;

internal static class ApsgConstants
{
    public const string PrimaryCommand = "/apsg";
    public const string AliasCommand = "/pvpseries";

    public const string LogPrefix = "[APSG]";

    // ContentRoulette row id for Crystalline Conflict (Casual Match), the roulette the source script queues.
    public const byte CasualMatchRouletteId = 40;

    // Crystalline Conflict objective, matched by BNpcName row id: the row carries a separate name per
    // client language, so comparing against the English text finds nothing on a non-English client.
    public const uint TacticalCrystalNameId = 11350;

    public const uint StatusSpawnProtection = 895; // active in the spawn pen before the gate opens
    public const uint StatusSprint = 1342;         // PvP sprint
    public const uint StatusGuard = 3054;          // PvP Guard (90% mitigation); poor target while up

    // Standard-issue Elixir (PvP): 4.5s cast that refills HP and MP to full. Any movement cancels it,
    // so the brain must hold still whenever the rotation solver is casting it.
    public const uint ActionStandardIssueElixir = 29055;

    public const int SaveThrottleMs = 500;

    internal static class CrystallineConflict
    {
        public const int MatchLengthSec = 300;

        public const int IntroBandUpperSec = 32;
        public const int IntroBandLowerSec = 1;
        public const int GateOpenSec = 100;
        public const int BaselineMovedThresholdSec = 10;
    }

    internal static class ThrottleKeys
    {
        public const string Save = "AutoPvpSeriesGrind.Save";
    }

    internal static class AddonNames
    {
        public const string MatchResults = "MKSRecord"; // PvP results screen
        public const string SelectYesno = "SelectYesno"; // generic yes/no confirm (e.g. logout)
        public const string DutyReady = "ContentsFinderConfirm"; // "Duty Ready" popup with the Commence button
    }

    // ClassJob.Role ids; used to pick a combat posture for the local player.
    internal static class JobRoles
    {
        public const int Tank = 1;
        public const int MeleeDps = 2;
        public const int RangedDps = 3; // physical + caster ranged
        public const int Healer = 4;
    }

    internal static class GameCommands
    {
        public const string RotationPreset = "LowHP";
        public const string EnableRotation = $"/rotation auto {RotationPreset}";
        public const string EnableRotationManual = "/rotation manual";
        public const string AddLowHpTargeting = $"/rotation Settings TargetingTypes add {RotationPreset}";

        public const string Sprint = "/pvpac sprint";
        public const string ClearEnemySignOnSelf = "/enemysign clear <me>";

        // Default emotes only; reward emotes the account may not own fail silently.
        public static readonly string[] GreetEmotes = ["/wave", "/cheer", "/salute", "/thumbsup", "/joy", "/stretch"];

        public const string Logout = "/logout";
        public const string CloseGame = "/xlkill";

        public const string NavStop = "/vnav stop";

        public static string NavMoveTo(Vector3 dest)
            => FormattableString.Invariant($"/vnav moveto {dest.X} {dest.Y} {dest.Z}");
    }

    internal static class LifestreamCommands
    {
        public const string ReturnToInn = "inn";
    }

    internal static class IpcGates
    {
        public const string NavMoveTo = "vnavmesh.SimpleMove.PathfindAndMoveTo";
        public const string NavMoveCloseTo = "vnavmesh.SimpleMove.PathfindAndMoveCloseTo";
        public const string NavStop = "vnavmesh.Path.Stop";
        public const string NavIsRunning = "vnavmesh.Path.IsRunning";
        public const string NavPathfindInProgress = "vnavmesh.SimpleMove.PathfindInProgress";
        public const string NavNearestPointReachable = "vnavmesh.Query.Mesh.NearestPointReachable";
        public const string NavIsReady = "vnavmesh.Nav.IsReady";
        public const string NavBuildProgress = "vnavmesh.Nav.BuildProgress";

        public const string LifestreamExecuteCommand = "Lifestream.ExecuteCommand";
        public const string LifestreamIsBusy = "Lifestream.IsBusy";

        public const string PvpAutoLbApiVersion = "PvpAutoLb.Presets.ApiVersion";
        public const string PvpAutoLbGetVersion = "PvpAutoLb.Presets.GetVersion";
        public const string PvpAutoLbApply = "PvpAutoLb.Presets.Apply";
    }

    // Type-name fragment identifying vnavmesh's IPC provider in faulted-task exception text.
    public const string VnavmeshIpcProviderMarker = "Navmesh.IPCProvider";

    public const int PvpAutoLbPresetApiVersion = 1;
}
