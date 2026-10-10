using AutoPvpSeriesGrind.Core.Humanize;
using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct EscortStep(Vector3 Destination, float StopRange, bool Sprint, bool Settled);

internal sealed class CrystalEscort
{
    private const float MovingSpeedYalmsPerSec = 0.3f;
    private const float VelocitySmoothing = 0.35f;
    private const long MaxSampleGapMs = 1000;
    private const float MillisecondsPerSecond = 1000f;
    private const float LeadSeconds = 0.8f;
    private const float MaxLead = 1.5f;
    private const float AllySeparation = 1.6f;
    private const float SlotDriftYalmsPerSec = 0.6f;
    private const int SlotHoldMinMs = 5000;
    private const int SlotHoldMaxMs = 12000;
    private const float ArriveStopRange = 0.5f;
    private const float SettledRadius = 0.9f;
    private const float MinComfortRadius = 1.4f;
    private const float ComfortSpread = 1.2f;
    private const float MaxDistanceFromCrystal = 3.2f;
    private const float SprintBeyondYalms = 12f;

    private static readonly SlotShape SlotArea = new(new Vector2(0f, -0.15f), new Vector2(2.2f, 1.35f), 0.8f, 2.2f);

    private readonly DriftingSlot slot = new(SlotDriftYalmsPerSec, SlotHoldMinMs, SlotHoldMaxMs);
    private Vector3 lastCrystal;
    private long lastSampleMs;
    private Vector3 velocity;
    private Vector3 forward = Vector3.UnitZ;
    private bool moving = true;

    public void Reset()
    {
        lastSampleMs = 0;
        velocity = default;
        forward = Vector3.UnitZ;
        moving = true;
        slot.Reset();
    }

    public EscortStep Step(PvpSnapshot snapshot, Vector3 crystal, Vector3? enemyBase, in Persona persona)
    {
        var now = Environment.TickCount64;
        TrackVelocity(crystal, now);
        var advancing = UpdateForward(enemyBase is { } basePosition ? VectorMath.FlatDirection(basePosition - crystal) : null);
        var offset = slot.Step(in SlotArea, now);

        var lead = advancing ? MathF.Min(velocity.Length() * LeadSeconds, MaxLead) : 0f;
        var destination = crystal + VectorMath.RightOf(forward) * offset.X + forward * (offset.Y + lead);
        destination = Spacing.KeepApart(snapshot.Allies, destination, AllySeparation);

        var distance = VectorMath.HorizontalDistance(snapshot.Self, destination);
        var comfort = MinComfortRadius + (1f - persona.Restlessness) * ComfortSpread;
        var offCrystal = VectorMath.HorizontalDistance(snapshot.Self, crystal) > MaxDistanceFromCrystal;
        moving = moving ? distance > SettledRadius : distance > comfort || offCrystal;
        return new EscortStep(destination, ArriveStopRange, Sprint: distance > SprintBeyondYalms, Settled: !moving);
    }

    private void TrackVelocity(Vector3 crystal, long now)
    {
        var elapsedMs = now - lastSampleMs;
        if (lastSampleMs == 0 || elapsedMs > MaxSampleGapMs)
        {
            velocity = default;
        }
        else if (elapsedMs > 0)
        {
            var sample = (crystal - lastCrystal) / (elapsedMs / MillisecondsPerSecond);
            sample.Y = 0f;
            velocity = Vector3.Lerp(velocity, sample, VelocitySmoothing);
        }
        lastCrystal = crystal;
        lastSampleMs = now;
    }

    private bool UpdateForward(Vector3? pushDirection)
    {
        if (velocity.Length() >= MovingSpeedYalmsPerSec)
        {
            var travel = Vector3.Normalize(velocity);
            if (pushDirection is not { } towardEnemy || Vector3.Dot(travel, towardEnemy) > 0f)
            {
                forward = travel;
                return true;
            }
        }

        if (pushDirection is { } fallback)
        {
            forward = fallback;
        }
        return false;
    }
}
