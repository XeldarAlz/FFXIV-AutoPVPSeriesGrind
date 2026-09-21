using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Rotation;
using ECommons.Automation;
using System.Numerics;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class RotationController(PvpBrain brain)
{
    private readonly PvpBrain brain = brain;
    private readonly PvpRotationDriver driver = new();

    public bool Enabled { get; private set; } = true;

    private bool wasDead;
    private bool clearedSignThisLife;

    public void Reset()
    {
        wasDead = false;
        clearedSignThisLife = false;
    }

    public void Configure(bool enabled, in RotationSettings rotationSettings)
    {
        Enabled = enabled;
        driver.Configure(in rotationSettings);
    }

    public void OnMatchStart() => driver.OnAlive();

    public void TickDeathAndRespawn()
    {
        if (MatchState.LocalIsDead())
        {
            OnDeathDetected();
            return;
        }

        OnRespawnDetected();
    }

    private void OnDeathDetected()
    {
        if (wasDead)
        {
            return;
        }

        wasDead = true;
        clearedSignThisLife = false;
        brain.Reset();
        ApsgLog.Info("death detected -> waiting for respawn");
    }

    private void OnRespawnDetected()
    {
        if (!wasDead)
        {
            return;
        }

        wasDead = false;
        driver.OnAlive();
        ApsgLog.Info("respawn detected");
    }

    public RotationOutcome Drive(PvpSnapshot snapshot, ulong targetId, Posture posture, Vector3 moveDestination, Action holdStill)
        => Enabled ? driver.Tick(snapshot, targetId, brain.UnderBurst, posture, moveDestination, holdStill) : RotationOutcome.None;

    public void EnsureSignCleared()
    {
        if (clearedSignThisLife) return;
        Chat.ExecuteCommand(GameCommands.ClearEnemySignOnSelf);
        clearedSignThisLife = true;
    }
}
