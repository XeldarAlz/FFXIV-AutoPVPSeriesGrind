using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Humanize;

internal readonly record struct SlotShape(Vector2 Center, Vector2 HalfExtent, float MinRadius, float MaxRadius);

internal sealed class DriftingSlot(float driftYalmsPerSecond, int holdMinMs, int holdMaxMs)
{
    private const float MaxStepSeconds = 1f;
    private const float MillisecondsPerSecond = 1000f;
    private const float MinVectorSq = 0.0001f;

    private Vector2 current;
    private Vector2 target;
    private bool placed;
    private long retargetAtMs;
    private long lastStepAtMs;

    public void Reset()
    {
        placed = false;
        retargetAtMs = 0;
        lastStepAtMs = 0;
    }

    public Vector2 Step(in SlotShape shape, long nowMs)
    {
        var elapsedSeconds = lastStepAtMs == 0 ? 0f : MathF.Min(MaxStepSeconds, (nowMs - lastStepAtMs) / MillisecondsPerSecond);
        lastStepAtMs = nowMs;
        if (!placed || nowMs >= retargetAtMs)
        {
            target = Pick(in shape);
            retargetAtMs = nowMs + HumanTiming.SharedRandom.Next(holdMinMs, holdMaxMs + 1);
            if (!placed)
            {
                current = target;
                placed = true;
            }
        }

        current = VectorMath.MoveTowards(current, target, driftYalmsPerSecond * elapsedSeconds);
        return current;
    }

    private static Vector2 Pick(in SlotShape shape)
    {
        var random = HumanTiming.SharedRandom;
        var point = shape.Center + new Vector2(
            (float)(random.NextDouble() * 2.0 - 1.0) * shape.HalfExtent.X,
            (float)(random.NextDouble() * 2.0 - 1.0) * shape.HalfExtent.Y);
        var length = point.Length();
        if (length < shape.MinRadius)
        {
            return length * length > MinVectorSq ? point / length * shape.MinRadius : new Vector2(shape.MinRadius, 0f);
        }
        return length > shape.MaxRadius ? point / length * shape.MaxRadius : point;
    }
}
