using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Rotation;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class RotationController(PvpBrain brain)
{
    private readonly PvpBrain brain = brain;
    private readonly PvpRotationDriver driver = new();

    public bool Enabled { get; private set; } = true;

    private bool wasDead;

    public void Reset() => wasDead = false;

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
        brain.Reset();
        RunLog.Info("death detected -> waiting for respawn");
    }

    private void OnRespawnDetected()
    {
        if (!wasDead)
        {
            return;
        }

        wasDead = false;
        driver.OnAlive();
        RunLog.Info("respawn detected");
    }

    public RotationOutcome Drive(PvpSnapshot snapshot, ulong targetId, Posture posture, Vector3 moveDestination, bool underBurst, Action holdStill)
        => Enabled ? driver.Tick(snapshot, targetId, underBurst, posture, moveDestination, holdStill) : RotationOutcome.None;
}
