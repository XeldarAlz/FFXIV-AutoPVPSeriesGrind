using AutoPvpSeriesGrind.Core.Humanize;

namespace AutoPvpSeriesGrind.Core.Combat;

internal enum MountIntent : byte
{
    None,
    Mount,
    Dismount,
}

internal readonly record struct MountInputs(bool Mounted, bool CanMount, float NearestEnemyYalms, float TravelYalms, bool CrowdRiding);

internal readonly record struct MountDecision(MountIntent Intent, string Reason)
{
    public static readonly MountDecision Stay = new(MountIntent.None, string.Empty);
}

internal sealed class MountPolicy
{
    private const float MountClearanceYalms = 10f;
    private const float RidingTeamTravelYalms = 15f;
    private const int MinRideMs = 3000;
    private const int RemountCooldownMs = 8000;
    private const int RequestRetryMs = 2500;
    private const int HesitationReactions = 3;

    private bool wasMounted;
    private long mountedAtMs;
    private long dismountedAtMs;
    private long requestedAtMs;
    private long wantedSinceMs;
    private int hesitationMs;

    public void Reset()
    {
        wasMounted = false;
        mountedAtMs = 0;
        dismountedAtMs = 0;
        requestedAtMs = 0;
        wantedSinceMs = 0;
        hesitationMs = 0;
    }

    public MountDecision Decide(in MountInputs inputs, float mountDistance, float dangerRadius, in Persona persona, long nowMs)
    {
        TrackTransition(inputs.Mounted, nowMs);
        return inputs.Mounted
            ? WhileRiding(in inputs, dangerRadius, in persona, nowMs)
            : WhileOnFoot(in inputs, mountDistance, dangerRadius, in persona, nowMs);
    }

    private void TrackTransition(bool mounted, long nowMs)
    {
        if (mounted == wasMounted)
        {
            return;
        }

        wasMounted = mounted;
        if (mounted)
        {
            mountedAtMs = nowMs;
            return;
        }
        dismountedAtMs = nowMs;
    }

    private MountDecision WhileRiding(in MountInputs inputs, float dangerRadius, in Persona persona, long nowMs)
    {
        wantedSinceMs = 0;
        if (nowMs - requestedAtMs < RequestRetryMs)
        {
            return MountDecision.Stay;
        }
        if (inputs.NearestEnemyYalms <= dangerRadius)
        {
            return Request(MountIntent.Dismount, $"enemy {inputs.NearestEnemyYalms:F0}y away -> dismounting", nowMs);
        }
        if (nowMs - mountedAtMs < MinRideMs || inputs.CrowdRiding || inputs.TravelYalms > persona.DismountNearYalms)
        {
            return MountDecision.Stay;
        }
        return Request(MountIntent.Dismount, $"caught up with the team ({inputs.TravelYalms:F0}y to go) -> dismounting", nowMs);
    }

    private MountDecision WhileOnFoot(in MountInputs inputs, float mountDistance, float dangerRadius, in Persona persona, long nowMs)
    {
        var tripYalms = mountDistance * persona.MountDistanceScale;
        var worthRiding = inputs.TravelYalms >= tripYalms || (inputs.CrowdRiding && inputs.TravelYalms >= RidingTeamTravelYalms);
        var safe = inputs.NearestEnemyYalms > dangerRadius + MountClearanceYalms;
        if (!inputs.CanMount || !safe || !worthRiding || nowMs - dismountedAtMs < RemountCooldownMs)
        {
            wantedSinceMs = 0;
            return MountDecision.Stay;
        }

        if (wantedSinceMs == 0)
        {
            wantedSinceMs = nowMs;
            hesitationMs = persona.NextReactionMs() * HesitationReactions;
        }
        if (nowMs - wantedSinceMs < hesitationMs || nowMs - requestedAtMs < RequestRetryMs)
        {
            return MountDecision.Stay;
        }
        return Request(MountIntent.Mount, $"{inputs.TravelYalms:F0}y to go with no enemy close -> mounting up", nowMs);
    }

    private MountDecision Request(MountIntent intent, string reason, long nowMs)
    {
        requestedAtMs = nowMs;
        return new MountDecision(intent, reason);
    }
}
