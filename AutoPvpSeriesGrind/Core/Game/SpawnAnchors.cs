using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Game;

internal readonly record struct SpawnAnchors(Vector3 A, Vector3 B)
{
    public Vector3 Nearest(Vector3 from)
        => Vector3.Distance(from, A) <= Vector3.Distance(from, B) ? A : B;

    public TeamBases IdentifyBySpawn(Vector3 spawnPosition)
        => Vector3.Distance(spawnPosition, A) <= Vector3.Distance(spawnPosition, B)
            ? new TeamBases(A, B)
            : new TeamBases(B, A);
}

internal readonly record struct TeamBases(Vector3 Own, Vector3 Enemy);
