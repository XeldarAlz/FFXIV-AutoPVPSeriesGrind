using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using ECommons.Automation;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

// Keeps RotationSolver's auto-rotation alive across the match: re-applies it after a death/respawn,
// re-enables it as a live failsafe, and clears the enemy sign once per life. Owns all of the
// per-life rotation/death state so it resets in exactly one place.
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

    // Called when the gate opens and the rotation has just been enabled for the opening life.
    public void MarkRotationEnabled()
    {
        rotationEnabledThisLife = true;
        rotationNeedsReset = false;
    }

    // Marks the rotation as needing a fresh enable on respawn (called while dead during a live match).
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

    // Live failsafe: if alive and the rotation hasn't been (re)enabled this life, enable it now.
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
