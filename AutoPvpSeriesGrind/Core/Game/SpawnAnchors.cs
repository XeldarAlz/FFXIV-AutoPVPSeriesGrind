using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal readonly record struct SpawnAnchors(Vector3 A, Vector3 B)
{
    public Vector3 Nearest(Vector3 from)
        => Vector3.Distance(from, A) <= Vector3.Distance(from, B) ? A : B;

    public Vector3? WithinArrival(Vector3 from, float radius)
    {
        Vector3? hit = null;
        if (Vector3.Distance(from, A) < radius) hit = A;
        if (Vector3.Distance(from, B) < radius) hit = B;
        return hit;
    }
}
