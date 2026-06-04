using AutoPvpSeriesGrind.Core.Game;
using ECommons.DalamudServices;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoPvpSeriesGrind.Core.Tasks;

public sealed partial class AutoPvpSeries
{
    // Distance from the spawn anchor at which we consider ourselves on that side and stop fleeing.
    private const float AnchorArriveRadius = 40f;
    private const float CrystalEngageRadius = 10f;
    private const int SpawnSafetyPollMs = 300;
    private const int LbFireEveryTicks = 5;

    private async Task TickLiveMatch()
    {
        CheckDeathAndReapplyRotation();
        if (!IsDead())
            Cmd("/enemysign clear <me>");

        if (IsNormal() && !IsDead() && !hasEnabledRotationThisLife)
        {
            Cmd("/rotation auto LowHP");
            hasEnabledRotationThisLife = true;
            Diag("rotation enabled (live failsafe)");
        }
        if (IsDead())
            hasEnabledRotationThisLife = false;

        var territory = Svc.ClientState.TerritoryType;

        var crystal = MatchState.CrystalPosition();
        if (crystal is { } c)
        {
            var player = MatchState.PlayerPosition();
            var crystalNear = player is { } p && Vector3.Distance(p, c) < CrystalEngageRadius;
            var shouldHold = crystalNear && MatchState.CrystalContested(c, CrystalEngageRadius);
            if (!shouldHold)
            {
                var rX = rng.Next(0, 3);
                var rZ = rng.Next(0, 3);
                MoveTo(c.X + rX, c.Y, c.Z + rZ);
            }
        }

        await RunSpawnSafetyLoop(territory);

        lbTick++;
        if (lbTick > LbFireEveryTicks && InDuty() && !IsDead())
        {
            lbTick = 0;
            foreach (var name in LimitBreakCatalog.NamesForJob(MatchState.LocalJobId()))
            {
                Cmd($"/pvpac \"{name}\"");
                await NextFrame(PollMs);
            }
        }
    }

    // While spawn protection (status 895) is up, sprint and walk to the nearest spawn-side anchor so we
    // leave the pen before the gate drops.
    private async Task RunSpawnSafetyLoop(uint territory)
    {
        var spawnSide = -1;
        while (MatchState.HasStatus(ApsgConstants.StatusSpawnProtection)
               && InDuty() && spawnSide == -1 && inMatchLive
               && !CancelToken.IsCancellationRequested)
        {
            CheckDeathAndReapplyRotation();
            if (!MatchState.HasStatus(ApsgConstants.StatusSprint))
                Cmd("/pvpac sprint");
            Cmd("/vnav stop");

            if (MatchState.SafeAnchors.TryGetValue(territory, out var a) && MatchState.PlayerPosition() is { } pos)
            {
                var dA = Vector3.Distance(pos, new Vector3(a[0], a[1], a[2]));
                var dB = Vector3.Distance(pos, new Vector3(a[3], a[4], a[5]));
                if (dA < AnchorArriveRadius) spawnSide = 0;
                if (dB < AnchorArriveRadius) spawnSide = 3;
                if (spawnSide > -1)
                {
                    ranSafetyMoveThisDuty = true;
                    MoveTo(a[spawnSide], a[spawnSide + 1], a[spawnSide + 2]);
                }
            }

            await NextFrame(SpawnSafetyPollMs);
        }
    }

    // Invariant formatting so locales with comma decimals don't corrupt the vnavmesh coordinates.
    private static void MoveTo(float x, float y, float z)
        => Cmd(FormattableString.Invariant($"/vnavmesh moveto {x} {y} {z}"));
}
