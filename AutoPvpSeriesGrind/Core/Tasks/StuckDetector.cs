using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class StuckDetector
{
    private const float ProgressEpsilonYalms = 1f;
    private const int StuckAfterMs = 2500;

    private Vector3 lastProgressPosition;
    private long lastProgressAtMs;

    public void Reset()
    {
        lastProgressPosition = default;
        lastProgressAtMs = 0;
    }

    public bool IsStuck(Vector3 self, bool pathing)
    {
        var now = Environment.TickCount64;
        if (!pathing || HasProgressed(self))
        {
            MarkProgress(self, now);
            return false;
        }
        return now - lastProgressAtMs >= StuckAfterMs;
    }

    private bool HasProgressed(Vector3 self)
        => lastProgressAtMs == 0 || Vector3.Distance(self, lastProgressPosition) > ProgressEpsilonYalms;

    private void MarkProgress(Vector3 position, long now)
    {
        lastProgressPosition = position;
        lastProgressAtMs = now;
    }
}
