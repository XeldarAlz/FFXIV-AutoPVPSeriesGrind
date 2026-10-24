using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

// Puts the limit break's target under the cursor while it fires, then hands the player's own target back unless
// something else has retargeted since.
internal sealed class TargetSwapper
{
    private const long RestoreDelayMs = 700;

    private ulong? originalTargetId;
    private ulong? swappedTargetId;
    private long swappedAtMs;

    public void Swap(IBattleChara target)
    {
        var current = Svc.Targets.Target;
        if (current?.EntityId == target.EntityId)
        {
            return;
        }

        if (originalTargetId is null && current is IBattleChara previous)
        {
            originalTargetId = previous.EntityId;
        }

        Svc.Targets.Target = target;
        swappedTargetId = target.EntityId;
        swappedAtMs = Environment.TickCount64;
    }

    public void TryRestore()
    {
        if (originalTargetId is not { } originalId || Environment.TickCount64 - swappedAtMs < RestoreDelayMs)
        {
            return;
        }

        if (Svc.Targets.Target?.EntityId == swappedTargetId)
        {
            Svc.Targets.Target = Svc.Objects.SearchById(originalId) is IBattleChara { IsDead: false } original ? original : null;
        }

        originalTargetId = null;
        swappedTargetId = null;
    }
}
