using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class SpotMemory(float radiusYalms, int rememberMs)
{
    private const int Capacity = 16;

    private readonly Vector3[] spots = new Vector3[Capacity];
    private readonly Vector3[] approaches = new Vector3[Capacity];
    private readonly long[] expiresAtMs = new long[Capacity];
    private int nextSlot;

    public void Clear() => Array.Clear(expiresAtMs);

    public void Remember(Vector3 spot, Vector3 approach, long nowMs)
    {
        var slot = IndexOf(spot, nowMs);
        if (slot < 0)
        {
            slot = nextSlot;
            nextSlot = (nextSlot + 1) % Capacity;
        }

        spots[slot] = spot;
        approaches[slot] = approach;
        expiresAtMs[slot] = nowMs + rememberMs;
    }

    public bool Contains(Vector3 point, long nowMs) => IndexOf(point, nowMs) >= 0;

    public bool TryRecall(Vector3 point, long nowMs, out Vector3 approach)
    {
        var slot = IndexOf(point, nowMs);
        approach = slot >= 0 ? approaches[slot] : default;
        return slot >= 0;
    }

    private int IndexOf(Vector3 point, long nowMs)
    {
        for (var slot = 0; slot < Capacity; slot++)
        {
            if (expiresAtMs[slot] > nowMs && Vector3.Distance(spots[slot], point) <= radiusYalms)
            {
                return slot;
            }
        }
        return -1;
    }
}
