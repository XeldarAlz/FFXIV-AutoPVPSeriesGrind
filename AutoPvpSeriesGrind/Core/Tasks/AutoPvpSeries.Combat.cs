using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Rotation;
using AutoPvpSeriesGrind.Core.Util;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using System.Numerics;
using System.Threading.Tasks;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed partial class AutoPvpSeries
{
    private const float CrystalEngageRadiusYalms = 10f;
    private const float LegacyCrystalStopRange = 1.5f;

    private const float SpawnExitArrivalRange = 3.5f;
    private const float ObjectiveFallbackStopRange = 3f;
    private const int SpawnExitTimeoutMs = 8000;
    private const float RespawnMinTravelYalms = 30f;
    private const int RespawnHesitationReactions = 4;
    private const float GateSnapExtentXZ = 1f;
    private const float GateSnapExtentY = 1.5f;
    private const float GateSnapToleranceYalms = 1f;

    private async Task TickLiveMatch()
    {
        if (TrackSteering())
        {
            TickPlayerSteering();
            return;
        }

        if (matchType == MatchType.Frontline)
        {
            await TickFrontlineLive();
            return;
        }

        rotation.TickDeathAndRespawn();
        if (IsDead())
        {
            movement.Stop();
            matchFlow.LeftSpawn = false;
            matchFlow.LeaveSpawnStartedAtMs = 0;
            matchFlow.AwaitingRespawnExit = true;
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Retreat, "dead, waiting to respawn", Posture.Retreat);
            return;
        }

        ScheduleRespawnExit();
        signClearer.Tick();

        var territory = Svc.ClientState.TerritoryType;

        if (MatchState.HasStatus(PvpStatuses.SpawnProtection))
        {
            MovementExecutor.EnsureSprinting();
            matchFlow.RanSafetyMoveThisDuty = true;
            CaptureBasesAtSpawn(territory);
        }

        if (MatchState.LocalIsCasting())
        {
            movement.Stop();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Hold, "wait: casting", Posture.Hold);
            return;
        }

        if (!matchFlow.LeftSpawn && (HesitatingInSpawn() || !TryLeaveSpawn(territory)))
            return;

        if (settings.EnableBrain)
            await RunBrainTick(territory);
        else
            LegacyCrystalMove();
    }

    private bool TrackSteering()
    {
        var steering = Plugin.Cfg.ManualMovement;
        if (steering == playerSteering)
        {
            return steering;
        }

        playerSteering = steering;
        movement.Stop();
        LogDiagnostic(steering
            ? "manual movement on -> you steer, the plugin keeps targeting and fighting"
            : "manual movement off -> the plugin steers again");
        return steering;
    }

    private void TickPlayerSteering()
    {
        rotation.TickDeathAndRespawn();
        if (IsDead())
        {
            if (matchType == MatchType.Frontline)
            {
                matchFlow.FrontlineDeathSpot ??= MatchState.PlayerPosition();
                frontline.OnDeath();
            }
            manual.Reset();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Retreat, "dead, waiting to respawn", Posture.Retreat);
            return;
        }

        signClearer.Tick();
        var snapshot = MatchState.Capture();
        KeepAutopilotReady(snapshot.Self);
        if (MatchState.LocalIsCasting())
        {
            BrainTelemetry.RecordStatus(snapshot, MoveKind.Hold, "you steer, casting", Posture.Hold);
            return;
        }

        var targetId = manual.PickTarget(snapshot, settings.PicksTargets, settings.Targeting, MatchState.HostileTargetId());
        var moving = Player.IsMoving;
        var posture = moving ? Posture.Idle : Posture.Hold;
        var reason = targetId == 0 ? "you steer, nothing in reach" : moving ? "you steer, instant skills on the move" : "you steer, free to cast";
        BrainTelemetry.Record(snapshot, new MovePlan(MoveKind.Hold, snapshot.Self, snapshot.Self, 0f, Sprint: false, reason,
            Pursue: false, posture, targetId));

        if (settings.PicksTargets && targetId != 0)
        {
            MatchState.SetTarget(targetId);
        }
        rotation.Drive(snapshot, targetId, posture, snapshot.Self, manual.UnderBurst, holdStill);
    }

    private void KeepAutopilotReady(Vector3 self)
    {
        if (matchType == MatchType.Frontline)
        {
            TrackFrontlineBase(self);
            return;
        }

        var inSpawn = MatchState.HasStatus(PvpStatuses.SpawnProtection);
        if (inSpawn)
        {
            CaptureBasesAtSpawn(Svc.ClientState.TerritoryType);
        }
        matchFlow.LeftSpawn = !inSpawn;
        matchFlow.LeaveSpawnStartedAtMs = 0;
        matchFlow.LeaveSpawnNotBeforeMs = 0;
        matchFlow.AwaitingRespawnExit = false;
    }

    private void ScheduleRespawnExit()
    {
        if (!matchFlow.AwaitingRespawnExit)
        {
            return;
        }

        matchFlow.AwaitingRespawnExit = false;
        matchFlow.LeaveSpawnNotBeforeMs = Environment.TickCount64 + persona.NextReactionMs() * RespawnHesitationReactions;
    }

    private bool HesitatingInSpawn()
    {
        if (Environment.TickCount64 >= matchFlow.LeaveSpawnNotBeforeMs)
        {
            return false;
        }

        BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Hold, "taking a moment before leaving spawn", Posture.Idle);
        return true;
    }

    private async Task TickFrontlineLive()
    {
        rotation.TickDeathAndRespawn();
        if (IsDead())
        {
            matchFlow.FrontlineDeathSpot ??= MatchState.PlayerPosition();
            frontline.OnDeath();
            movement.Stop();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Retreat, "dead, waiting to respawn", Posture.Retreat);
            return;
        }

        signClearer.Tick();

        if (MatchState.LocalIsCasting())
        {
            movement.Stop();
            BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Hold, "wait: casting", Posture.Hold);
            return;
        }

        var snapshot = MatchState.Capture();
        TrackFrontlineBase(snapshot.Self);
        var plan = frontline.Decide(snapshot);
        BrainTelemetry.Record(snapshot, plan);
        movement.SetObstacles(snapshot.Structures);

        if (HandleMount(snapshot, in plan))
        {
            return;
        }
        var rotationPosture = frontline.TeamOnTheMove ? Posture.Idle : plan.Posture;
        if (!Svc.Condition[ConditionFlag.Mounted]
            && HoldsStillForRotation(snapshot, plan.TargetId, rotationPosture, plan.Destination, frontline.UnderBurst))
        {
            return;
        }

        await ReactToPostureChange(movement.UpdatePosture(plan.Posture));
        ApplyBrainTarget(plan.TargetId);
        movement.Execute(plan);
    }

    private void TrackFrontlineBase(Vector3 self)
    {
        if (!matchFlow.FrontlineBaseChecked)
        {
            matchFlow.FrontlineBaseChecked = true;
            if (matchFlow.DutyBaselineTime == 0)
            {
                frontline.MarkBase(self);
                LogDiagnostic($"Frontline: own base marked at the start position {self:F0}");
            }
        }

        if (matchFlow.FrontlineDeathSpot is not { } deathSpot)
        {
            return;
        }

        matchFlow.FrontlineDeathSpot = null;
        if (Vector3.Distance(self, deathSpot) > RespawnMinTravelYalms)
        {
            frontline.MarkBase(self);
            LogDiagnostic($"Frontline: own base marked at the respawn position {self:F0}");
        }
    }

    private bool HandleMount(PvpSnapshot snapshot, in MovePlan plan)
    {
        var inputs = new MountInputs(
            Mounted: Svc.Condition[ConditionFlag.Mounted],
            CanMount: !Svc.Condition[ConditionFlag.InCombat] && ActionOps.IsGeneralActionReady(GeneralActions.MountRoulette),
            NearestEnemyYalms: snapshot.NearestEnemyDistance,
            TravelYalms: VectorMath.HorizontalDistance(snapshot.Self, plan.Destination),
            CrowdRiding: frontline.CrowdRiding);
        var decision = frontline.DecideMount(in inputs, Environment.TickCount64);
        switch (decision.Intent)
        {
            case MountIntent.Mount:
                movement.Stop();
                ActionOps.UseGeneralAction(GeneralActions.MountRoulette);
                break;
            case MountIntent.Dismount:
                ActionOps.UseGeneralAction(GeneralActions.Dismount);
                break;
            default:
                return false;
        }

        LogDiagnostic(decision.Reason);
        return true;
    }

    private void CaptureBasesAtSpawn(uint territory)
    {
        if (matchFlow.Bases is null && MatchState.PlayerPosition() is { } spawnPosition)
        {
            matchFlow.Bases = MatchState.IdentifyBases(territory, spawnPosition);
            if (matchFlow.Bases is { } bases)
                LogDiagnostic($"bases identified: own={bases.Own:F0} enemy={bases.Enemy:F0}");
        }
    }

    private Vector3? SpawnExit(uint territory, Vector3 self)
    {
        if (matchFlow.Bases is not { } bases)
        {
            return MatchState.NearestSafeAnchor(territory, self);
        }
        if (matchFlow.GateTarget is { } cached)
        {
            return cached;
        }
        if (SpreadAlongGate(bases) is not { } spread)
        {
            return bases.Own;
        }

        matchFlow.GateTarget = spread;
        return spread;
    }

    private Vector3? SpreadAlongGate(TeamBases bases)
    {
        if (persona.GateLateralYalms == 0f || VectorMath.FlatDirection(bases.Enemy - bases.Own) is not { } axis)
        {
            return bases.Own;
        }

        var nav = NavIpc.Instance;
        if (!nav.IsReady())
        {
            return null;
        }

        var candidate = bases.Own + VectorMath.RightOf(axis) * persona.GateLateralYalms;
        return nav.NearestPointReachable(candidate, GateSnapExtentXZ, GateSnapExtentY) is { } snapped
               && VectorMath.HorizontalDistance(snapped, candidate) <= GateSnapToleranceYalms
            ? snapped
            : bases.Own;
    }

    // Players are already pressed against the barrier when it drops. Requesting the gate path a few
    // seconds early hides pathfind latency, so the character is moving the moment the gate opens.
    private void ApproachGate(uint territory)
    {
        if (MatchState.PlayerPosition() is not { } self || SpawnExit(territory, self) is not { } exit)
        {
            return;
        }

        movement.IssueMove(exit, exit, SpawnExitArrivalRange);
        BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Engage, "gate opening soon, moving up", Posture.Reposition);
    }

    private bool TryLeaveSpawn(uint territory)
    {
        if (MatchState.PlayerPosition() is not { } self)
            return true;

        if (SpawnExit(territory, self) is not { } exit)
        {
            matchFlow.LeftSpawn = true;
            return true;
        }

        var now = Environment.TickCount64;
        var moveIssued = matchFlow.LeaveSpawnStartedAtMs != 0;
        if (!moveIssued)
        {
            matchFlow.LeaveSpawnStartedAtMs = now;
        }

        var flat = VectorMath.HorizontalDistance(self, exit);
        var arrived = flat <= SpawnExitArrivalRange;
        var pathEnded = moveIssued && !movement.IsPathing;
        var timedOut = now - matchFlow.LeaveSpawnStartedAtMs > SpawnExitTimeoutMs;
        if (arrived || pathEnded || timedOut)
        {
            matchFlow.LeftSpawn = true;
            LogDiagnostic(SpawnHandoffReason(arrived, pathEnded, flat));
            return true;
        }

        movement.IssueMove(exit, exit, SpawnExitArrivalRange);
        BrainTelemetry.RecordStatus(MatchState.Capture(), MoveKind.Engage, "leaving spawn, to gate anchor", Posture.Reposition);
        return false;
    }

    private static string SpawnHandoffReason(bool arrived, bool pathEnded, float flat)
    {
        if (arrived)
        {
            return $"off the spawn platform (anchor {flat:F1}y) -> brain takes over";
        }

        if (pathEnded)
        {
            return $"spawn exit path ended {flat:F1}y from anchor -> brain takes over";
        }

        return $"leave-spawn timeout ({SpawnExitTimeoutMs}ms, {flat:F1}y out) -> handing off to brain";
    }

    private async Task RunBrainTick(uint territory)
    {
        var snapshot = MatchState.Capture();

        if (!snapshot.HasObjective)
        {
            WarnMissingObjectiveOnce();
            if (HoldsStillForRotation(snapshot, 0, Posture.Reposition, snapshot.Self, brain.UnderBurst))
            {
                return;
            }
            AdvanceWithoutObjective(snapshot);
            return;
        }

        matchFlow.LoggedMissingObjective = false;

        var anchor = matchFlow.Bases?.Own ?? MatchState.NearestSafeAnchor(territory, snapshot.Self) ?? snapshot.Self;
        var plan = brain.Decide(snapshot, anchor, matchFlow.Bases?.Enemy);
        BrainTelemetry.Record(snapshot, plan);

        if (HoldsStillForRotation(snapshot, plan.TargetId, plan.Posture, plan.Destination, brain.UnderBurst))
        {
            return;
        }

        await ReactToPostureChange(movement.UpdatePosture(plan.Posture));

        ApplyBrainTarget(plan.TargetId);
        movement.Execute(plan);
    }

    // The crystal starts at the arena center, midway along the line joining the two bases. Standing
    // still until it resolves is what made the bot look parked at the spawn exit, so walk the line instead.
    private void AdvanceWithoutObjective(in PvpSnapshot snapshot)
    {
        if (matchFlow.Bases is not { } bases)
        {
            BrainTelemetry.Record(snapshot, new MovePlan(MoveKind.Hold, snapshot.Self, snapshot.Self, 0f, false, "no objective"));
            movement.HaltPathing();
            return;
        }

        var crystalLineCenter = Vector3.Lerp(bases.Own, bases.Enemy, 0.5f);
        BrainTelemetry.RecordStatus(snapshot, MoveKind.Engage, "no objective yet, advancing to crystal line", Posture.Reposition);
        movement.IssueMove(crystalLineCenter, bases.Own, ObjectiveFallbackStopRange);
    }

    private void WarnMissingObjectiveOnce()
    {
        if (matchFlow.LoggedMissingObjective)
        {
            return;
        }

        matchFlow.LoggedMissingObjective = true;
        Warn($"objective not found among {Svc.Objects.Length} loaded objects (Tactical Crystal, BNpcName {TacticalCrystalNameId})");
    }

    private bool HoldsStillForRotation(PvpSnapshot snapshot, ulong targetId, Posture posture, Vector3 moveDestination, bool underBurst)
    {
        var outcome = rotation.Drive(snapshot, targetId, posture, moveDestination, underBurst, holdStill);
        if (outcome != RotationOutcome.Cast)
        {
            return false;
        }

        movement.Stop();
        BrainTelemetry.RecordStatus(snapshot, MoveKind.Hold, "casting, holding still", Posture.Hold);
        return true;
    }

    private void ApplyBrainTarget(ulong targetId)
    {
        if (settings.BrainTargets && targetId != 0)
        {
            MatchState.SetTarget(targetId);
        }
    }

    private void LegacyCrystalMove()
    {
        var snapshot = MatchState.Capture();
        if (snapshot.Objective is not { } crystalPosition)
        {
            WarnMissingObjectiveOnce();
            BrainTelemetry.RecordStatus(snapshot, MoveKind.Hold, "no objective (legacy)");
            return;
        }

        var enemyOnPoint = false;
        for (var enemyIndex = 0; enemyIndex < snapshot.Enemies.Count; enemyIndex++)
        {
            var enemy = snapshot.Enemies[enemyIndex];
            if (Vector3.Distance(enemy.Position, crystalPosition) < CrystalEngageRadiusYalms)
            {
                enemyOnPoint = true;
                break;
            }
        }

        if (HazardAvoidance.TryDodge(snapshot, crystalPosition, out var exit, out var hazard))
        {
            BrainTelemetry.RecordStatus(snapshot, MoveKind.Retreat, $"dodge {hazard.Source} (legacy)", Posture.Reposition);
            MovementExecutor.EnsureSprinting();
            movement.IssueMove(exit, exit, LegacyCrystalStopRange);
            return;
        }

        var hold = Vector3.Distance(snapshot.Self, crystalPosition) < CrystalEngageRadiusYalms && enemyOnPoint;
        if (HoldsStillForRotation(snapshot, snapshot.CurrentTarget?.Id ?? 0, hold ? Posture.Hold : Posture.Push, crystalPosition, brain.UnderBurst))
        {
            return;
        }
        BrainTelemetry.RecordStatus(snapshot, hold ? MoveKind.Hold : MoveKind.Engage, hold ? "hold (legacy)" : "to crystal (legacy)");
        if (!hold)
            movement.IssueMove(crystalPosition, crystalPosition, LegacyCrystalStopRange);
        else
            movement.Stop();
    }
}
