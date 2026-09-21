using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal enum HazardShape : byte
{
    Circle,
    Cone,
    Line,
}

internal readonly record struct Hazard(HazardShape Shape, Vector3 Origin, Vector3 Direction, float Length, float HalfWidth, string Source)
{
    private const float ConeHalfAngleRadians = MathF.PI / 3f;
    private const float MinVectorSq = 0.0001f;

    public bool Contains(Vector3 point, float margin)
    {
        var offset = Flat(point - Origin);
        switch (Shape)
        {
            case HazardShape.Circle:
                return offset.LengthSquared() <= Square(Length + margin);
            case HazardShape.Cone:
                var distance = offset.Length();
                if (distance > Length + margin)
                {
                    return false;
                }
                return distance * distance <= MinVectorSq || Vector3.Dot(offset / distance, Direction) >= MathF.Cos(ConeHalfAngleRadians);
            default:
                var along = Vector3.Dot(offset, Direction);
                if (along < -margin || along > Length + margin)
                {
                    return false;
                }
                return (offset - Direction * along).LengthSquared() <= Square(HalfWidth + margin);
        }
    }

    public Vector3 ExitPoint(Vector3 point, Vector3 preferTowards, float clearance)
    {
        var offset = Flat(point - Origin);
        if (Shape == HazardShape.Circle)
        {
            var away = offset.LengthSquared() > MinVectorSq ? Vector3.Normalize(offset) : FallbackDirection(Flat(preferTowards - Origin));
            return WithHeight(Origin + away * (Length + clearance), point.Y);
        }

        var along = Vector3.Dot(offset, Direction);
        var perpendicular = offset - Direction * along;
        var perpendicularDistance = perpendicular.Length();
        var side = perpendicularDistance * perpendicularDistance > MinVectorSq
            ? perpendicular / perpendicularDistance
            : SideToward(Flat(preferTowards - point));
        var limit = Shape == HazardShape.Line ? HalfWidth : MathF.Max(0f, along) * MathF.Tan(ConeHalfAngleRadians);
        return WithHeight(point + side * (limit + clearance - perpendicularDistance), point.Y);
    }

    private Vector3 SideToward(Vector3 preference)
    {
        var left = new Vector3(-Direction.Z, 0f, Direction.X);
        return Vector3.Dot(left, preference) >= 0f ? left : -left;
    }

    private static Vector3 FallbackDirection(Vector3 preference)
        => preference.LengthSquared() > MinVectorSq ? Vector3.Normalize(preference) : Vector3.UnitX;

    private static Vector3 Flat(Vector3 vector) => vector with { Y = 0f };

    private static Vector3 WithHeight(Vector3 vector, float height) => vector with { Y = height };

    private static float Square(float value) => value * value;
}

internal static class HazardAvoidance
{
    private const float MarginYalms = 1.5f;
    private const float ClearanceYalms = 3f;

    public static bool TryDodge(PvpSnapshot snapshot, Vector3 preferTowards, out Vector3 exit, out Hazard hazard)
    {
        if (snapshot.InHazard(snapshot.Self, MarginYalms, out hazard))
        {
            exit = hazard.ExitPoint(snapshot.Self, preferTowards, ClearanceYalms);
            return true;
        }

        exit = default;
        return false;
    }

    public static Vector3 KeepClear(PvpSnapshot snapshot, Vector3 destination, out Hazard? skirted)
    {
        if (snapshot.InHazard(destination, MarginYalms, out var hazard))
        {
            skirted = hazard;
            return hazard.ExitPoint(destination, snapshot.Self, ClearanceYalms);
        }

        skirted = null;
        return destination;
    }
}
