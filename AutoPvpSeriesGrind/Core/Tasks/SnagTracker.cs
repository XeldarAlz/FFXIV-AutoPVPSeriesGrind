using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Tasks;

// Stalls are counted by where the player is stuck, not where it is heading: a team anchor moves every tick.
internal sealed class SnagTracker
{
    private const float SameSnagYalms = 4f;
    private const int SameSnagWindowMs = 30_000;
    private const float BackOffStepYalms = 3f;
    private const float SideStepYalms = 4f;
    private const int MaxBackOffSteps = 3;

    private Vector3 lastSnag;
    private long lastSnagAtMs;
    private int strikes;

    public int Strike(Vector3 self, long nowMs)
    {
        var repeated = strikes > 0
                       && nowMs - lastSnagAtMs < SameSnagWindowMs
                       && Vector3.Distance(self, lastSnag) <= SameSnagYalms;
        strikes = repeated ? strikes + 1 : 1;
        lastSnag = self;
        lastSnagAtMs = nowMs;
        return strikes;
    }

    public void Reset()
    {
        strikes = 0;
        lastSnagAtMs = 0;
    }

    public static Vector3 BackOffPoint(Vector3 self, Vector3 facing, int strikes, int side)
    {
        var steps = Math.Clamp(strikes - 1, 1, MaxBackOffSteps);
        return self - facing * (BackOffStepYalms * steps) + VectorMath.RightOf(facing) * (SideStepYalms * steps * side);
    }
}
