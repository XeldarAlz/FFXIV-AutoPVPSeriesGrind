using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using ECommons.Automation;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class RotationController(PvpBrain brain)
{
    // Grace period after death before re-applying the rotation, so it lands after the respawn completes.
    private const int RespawnRotationDelayMs = 10_000;

    private readonly PvpBrain brain = brain;

    private bool wasDead;
    private long deadSinceMs;
    private bool rotationNeedsReset;
    private bool rotationEnabledThisLife;
    private bool clearedSignThisLife;

    public void Reset()
    {
        wasDead = false;
        deadSinceMs = 0;
        rotationNeedsReset = false;
        rotationEnabledThisLife = false;
        clearedSignThisLife = false;
    }

    public void MarkRotationEnabled()
    {
        rotationEnabledThisLife = true;
        rotationNeedsReset = false;
    }

    public void OnDeadDuringLive() => rotationEnabledThisLife = false;

    public void TickDeathAndRespawn()
    {
        if (MatchState.LocalIsDead())
        {
            if (!wasDead)
            {
                wasDead = true;
                deadSinceMs = Environment.TickCount64;
                rotationNeedsReset = true;
                clearedSignThisLife = false;
                brain.Reset();
                ApsgLog.Info("death detected -> rotation will be re-applied after respawn");
            }
            return;
        }

        if (wasDead)
        {
            wasDead = false;
            if (rotationNeedsReset && Environment.TickCount64 - deadSinceMs >= RespawnRotationDelayMs && MatchState.IsNormalConditions())
            {
                Chat.ExecuteCommand(GameCommands.EnableRotation);
                rotationNeedsReset = false;
                ApsgLog.Info("respawn detected -> rotation re-applied");
            }
        }

        if (rotationNeedsReset && MatchState.IsNormalConditions())
        {
            Chat.ExecuteCommand(GameCommands.EnableRotation);
            rotationNeedsReset = false;
            ApsgLog.Info("rotation re-applied (failsafe)");
        }
    }

    public void EnsureRotationEnabled()
    {
        if (MatchState.IsNormalConditions() && !rotationEnabledThisLife)
        {
            Chat.ExecuteCommand(GameCommands.EnableRotation);
            rotationEnabledThisLife = true;
            ApsgLog.Info("rotation enabled (live failsafe)");
        }
    }

    public void EnsureSignCleared()
    {
        if (clearedSignThisLife) return;
        Chat.ExecuteCommand(GameCommands.ClearEnemySignOnSelf);
        clearedSignThisLife = true;
    }
}
