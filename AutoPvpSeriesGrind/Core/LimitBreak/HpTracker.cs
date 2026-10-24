using Dalamud.Game.ClientState.Objects.Types;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

// Watches each enemy's HP over a short window, so a target losing HP fast enough to die before the limit break
// lands can be passed over instead of wasting the charge on a kill someone else gets.
internal sealed class HpTracker
{
    private const long WindowMs = 1000;
    private const long MinimumSpanMs = 250;
    private const long PruneStaleAfterMs = 5000;
    private const double DoomedWithinMs = 1200;

    private readonly Dictionary<ulong, Window> windows = [];
    private readonly List<ulong> staleIds = [];
    private long lastPruneAtMs = Environment.TickCount64;

    private readonly record struct Window(uint OldHp, long OldAtMs, uint NewHp, long NewAtMs);

    public void Sample(IBattleChara target)
    {
        var now = Environment.TickCount64;
        var hp = target.CurrentHp;
        windows[target.EntityId] = windows.TryGetValue(target.EntityId, out var window) && now - window.OldAtMs <= WindowMs
            ? window with { NewHp = hp, NewAtMs = now }
            : new Window(hp, now, hp, now);

        if (now - lastPruneAtMs > PruneStaleAfterMs)
        {
            Prune(now);
            lastPruneAtMs = now;
        }
    }

    public bool IsDoomed(IBattleChara target)
    {
        if (!windows.TryGetValue(target.EntityId, out var window) || window.NewHp >= window.OldHp)
        {
            return false;
        }

        var spanMs = window.NewAtMs - window.OldAtMs;
        if (spanMs < MinimumSpanMs)
        {
            return false;
        }

        var hpPerMs = (double)(window.OldHp - window.NewHp) / spanMs;
        return target.CurrentHp / hpPerMs < DoomedWithinMs;
    }

    private void Prune(long now)
    {
        staleIds.Clear();
        foreach (var (entityId, window) in windows)
        {
            if (now - window.NewAtMs > PruneStaleAfterMs)
            {
                staleIds.Add(entityId);
            }
        }

        for (var staleIndex = 0; staleIndex < staleIds.Count; staleIndex++)
        {
            windows.Remove(staleIds[staleIndex]);
        }
    }
}
