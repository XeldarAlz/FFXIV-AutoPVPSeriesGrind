using AutoPvpSeriesGrind.Core.Util;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Combat;

internal readonly record struct EscortStep(Vector3 Destination, float StopRange, bool Walk, bool Sprint);

internal sealed class CrystalEscort
{
    private const float MovingSpeedYalmsPerSec = 0.3f;
    private const float VelocitySmoothing = 0.35f;
    private const long MaxSampleGapMs = 1000;
    private const float MinVectorSq = 0.01f;
    private const float MaxLateralOffset = 2.2f;
    private const float MaxForwardOffset = 1.2f;
    private const float MaxBackOffset = 1.5f;
    private const float MinOffsetFromCenter = 0.8f;
    private const float MaxOffsetFromCenter = 2.5f;
    private const float LeadSeconds = 0.8f;
    private const float MaxLead = 1.5f;
    private const float AllySeparation = 1.6f;
    private const float SlotDriftYalmsPerSec = 0.6f;
    private const int SlotHoldMinMs = 5000;
    private const int SlotHoldMaxMs = 12000;
    private const float MinStopRange = 0.6f;
    private const float MaxStopRange = 1.1f;
    private const float WalkWithinYalms = 5f;
    private const float SprintBeyondYalms = 12f;

    private Vector3 lastCrystal;
    private long lastSampleMs;
    private Vector3 velocity;
    private Vector3 forward = Vector3.UnitZ;
    private Vector2 slot;
    private Vector2 slotTarget;
    private bool hasSlot;
    private float stopRange = MaxStopRange;
    private long slotExpiresAtMs;

    public void Reset()
    {
        lastSampleMs = 0;
        velocity = default;
        forward = Vector3.UnitZ;
        slotExpiresAtMs = 0;
        hasSlot = false;
    }

    public EscortStep Step(PvpSnapshot snapshot, Vector3 crystal, Vector3? enemyBase)
    {
        var now = Environment.TickCount64;
        var elapsedSec = TrackVelocity(crystal, now);
        var advancing = UpdateForward(crystal, enemyBase);
        if (now >= slotExpiresAtMs)
        {
            PickSlot(now);
        }
        slot = MoveTowards(slot, slotTarget, SlotDriftYalmsPerSec * elapsedSec);

        var right = new Vector3(forward.Z, 0f, -forward.X);
        var lead = advancing ? MathF.Min(HorizontalSpeed() * LeadSeconds, MaxLead) : 0f;
        var destination = crystal + right * slot.X + forward * (slot.Y + lead);
        destination = SeparateFromAllies(snapshot, destination);

        var distance = VectorMath.HorizontalDistance(snapshot.Self, destination);
        return new EscortStep(destination, stopRange, Walk: distance <= WalkWithinYalms, Sprint: distance > SprintBeyondYalms);
    }

    private float TrackVelocity(Vector3 crystal, long now)
    {
        var elapsedMs = now - lastSampleMs;
        var elapsedSec = 0f;
        if (lastSampleMs == 0 || elapsedMs > MaxSampleGapMs)
        {
            velocity = default;
        }
        else if (elapsedMs > 0)
        {
            elapsedSec = elapsedMs / 1000f;
            var sample = (crystal - lastCrystal) / elapsedSec;
            sample.Y = 0f;
            velocity = Vector3.Lerp(velocity, sample, VelocitySmoothing);
        }
        lastCrystal = crystal;
        lastSampleMs = now;
        return elapsedSec;
    }

    private bool UpdateForward(Vector3 crystal, Vector3? enemyBase)
    {
        var pushDirection = enemyBase is { } basePosition ? Flatten(basePosition - crystal) : null;
        var moving = HorizontalSpeed() >= MovingSpeedYalmsPerSec;
        if (moving)
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

    private void PickSlot(long now)
    {
        var random = HumanTiming.SharedRandom;
        var lateral = (float)(random.NextDouble() * 2.0 - 1.0) * MaxLateralOffset;
        var along = (float)(random.NextDouble() * (MaxForwardOffset + MaxBackOffset)) - MaxBackOffset;
        var offset = new Vector2(lateral, along);
        var length = offset.Length();
        if (length < MinOffsetFromCenter)
        {
            offset = length * length > MinVectorSq ? offset / length * MinOffsetFromCenter : new Vector2(MinOffsetFromCenter, 0f);
        }
        else if (length > MaxOffsetFromCenter)
        {
            offset = offset / length * MaxOffsetFromCenter;
        }

        slotTarget = offset;
        if (!hasSlot)
        {
            slot = offset;
            hasSlot = true;
        }
        stopRange = MinStopRange + (float)random.NextDouble() * (MaxStopRange - MinStopRange);
        slotExpiresAtMs = now + random.Next(SlotHoldMinMs, SlotHoldMaxMs + 1);
    }

    private static Vector3 SeparateFromAllies(PvpSnapshot snapshot, Vector3 destination)
    {
        for (var allyIndex = 0; allyIndex < snapshot.Allies.Count; allyIndex++)
        {
            var away = destination - snapshot.Allies[allyIndex].Position;
            away.Y = 0f;
            var distance = away.Length();
            if (distance >= AllySeparation || distance * distance <= MinVectorSq)
            {
                continue;
            }
            destination += away / distance * (AllySeparation - distance);
        }
        return destination;
    }

    private static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxStep)
    {
        var delta = target - current;
        var distance = delta.Length();
        return distance <= maxStep || distance <= 0f ? target : current + delta / distance * maxStep;
    }

    private float HorizontalSpeed() => velocity.Length();

    private static Vector3? Flatten(Vector3 direction)
    {
        direction.Y = 0f;
        return direction.LengthSquared() > MinVectorSq ? Vector3.Normalize(direction) : null;
    }
}
