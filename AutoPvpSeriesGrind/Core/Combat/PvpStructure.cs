using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct PvpStructure(ulong Id, Vector3 Position, float Radius, float Hp, int AllyVotes);
