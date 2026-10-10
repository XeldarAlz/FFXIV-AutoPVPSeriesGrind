using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal static class Spacing
{
    private const float MinVectorSq = 0.01f;

    public static Vector3 KeepApart(IReadOnlyList<PvpActor> allies, Vector3 destination, float separation)
    {
        for (var allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            var away = destination - allies[allyIndex].Position;
            away.Y = 0f;
            var distance = away.Length();
            if (distance >= separation || distance * distance <= MinVectorSq)
            {
                continue;
            }
            destination += away / distance * (separation - distance);
        }
        return destination;
    }

    public static Vector3 KeepOutside(IReadOnlyList<PvpStructure> structures, Vector3 destination, Vector3 approachFrom, float clearance)
    {
        for (var structureIndex = 0; structureIndex < structures.Count; structureIndex++)
        {
            var structure = structures[structureIndex];
            var limit = structure.Radius + clearance;
            var away = destination - structure.Position;
            away.Y = 0f;
            var distance = away.Length();
            if (distance >= limit)
            {
                continue;
            }
            if (distance * distance <= MinVectorSq)
            {
                away = approachFrom - structure.Position;
                away.Y = 0f;
                distance = away.Length();
                if (distance * distance <= MinVectorSq)
                {
                    away = Vector3.UnitX;
                    distance = 1f;
                }
            }
            destination = (structure.Position + away / distance * limit) with { Y = destination.Y };
        }
        return destination;
    }
}
