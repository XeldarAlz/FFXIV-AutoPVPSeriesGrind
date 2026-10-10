using AutoPvpSeriesGrind.Core.Util;

namespace AutoPvpSeriesGrind.Core.Humanize;

internal enum GateStyle : byte
{
    OnTime,
    Eager,
    Late,
}

internal readonly record struct Persona(
    int ReactionMinMs,
    int ReactionMaxMs,
    float RouteWander,
    float CornerRounding,
    float MountDistanceScale,
    float DismountNearYalms,
    GateStyle Gate,
    float GateSeconds,
    float GateLateralYalms,
    float FormationLateralYalms,
    float FormationDepthYalms,
    float LeadScale,
    float Restlessness,
    float HopChance,
    bool JumpsLedges)
{
    public static readonly Persona Neutral = new(
        ReactionMinMs: 0,
        ReactionMaxMs: 0,
        RouteWander: 0f,
        CornerRounding: 0f,
        MountDistanceScale: 1f,
        DismountNearYalms: 15f,
        Gate: GateStyle.OnTime,
        GateSeconds: 2.5f,
        GateLateralYalms: 0f,
        FormationLateralYalms: 0f,
        FormationDepthYalms: 0f,
        LeadScale: 1f,
        Restlessness: 0f,
        HopChance: 0f,
        JumpsLedges: false);

    public int NextReactionMs() => HumanTiming.Reaction(ReactionMinMs, ReactionMaxMs);

    public string Describe()
        => $"reactions {ReactionMinMs}-{ReactionMaxMs}ms, gate {Gate} {GateSeconds:F1}s at {GateLateralYalms:+0.0;-0.0;0}y, "
         + $"slot {FormationLateralYalms:+0.0;-0.0;0}/{FormationDepthYalms:+0.0;-0.0;0}y, lead x{LeadScale:F2}, "
         + $"route wander {RouteWander:F2}, corners {CornerRounding:F1}y, mount x{MountDistanceScale:F2}, dismount {DismountNearYalms:F0}y, "
         + $"restless {Restlessness:F2}, hops {HopChance:P0}, ledge jumps {(JumpsLedges ? "on" : "off")}";
}

internal static class PersonaRoller
{
    private const float LightSpread = 0.6f;
    private const float RealisticSpread = 1f;
    private const float HeavySpread = 1.25f;

    private const float MinReactionScale = 0.8f;
    private const float MaxReactionScale = 1.35f;

    private const double DirectRunnerShare = 0.25;
    private const float DirectRunnerMinWander = 0.05f;
    private const float DirectRunnerMaxWander = 0.3f;
    private const float WandererMinWander = 0.45f;
    private const float WandererMaxWander = 1f;

    private const float MinCornerRounding = 0.3f;
    private const float MaxCornerRounding = 1.1f;

    private const float MinMountDistanceScale = 0.75f;
    private const float MaxMountDistanceScale = 1.2f;
    private const float MinDismountNearYalms = 9f;
    private const float MaxDismountNearYalms = 20f;

    private const double EagerShare = 0.3;
    private const double LateShare = 0.2;
    private const float EagerMinSeconds = 5f;
    private const float EagerMaxSeconds = 11f;
    private const float OnTimeMinSeconds = 0.6f;
    private const float OnTimeMaxSeconds = 3.8f;
    private const float LateMinSeconds = 0.5f;
    private const float LateMaxSeconds = 3.5f;
    private const float MinGateLateralYalms = 0.4f;
    private const float MaxGateLateralYalms = 2.4f;

    private const float MinFormationLateralYalms = 1.5f;
    private const float MaxFormationLateralYalms = 6.5f;
    private const float MaxFormationDepthYalms = 2f;
    private const float MinLeadScale = 0.6f;
    private const float MaxLeadScale = 1.3f;

    private const float MinRestlessness = 0.15f;
    private const float MaxRestlessness = 0.9f;

    private const double HopperShare = 0.3;
    private const float MinHopChance = 0.08f;
    private const float MaxHopChance = 0.3f;
    private const double LedgeJumperShare = 0.85;

    public static Persona Roll(HumanizeLevel level)
    {
        var spread = Spread(level);
        if (spread <= 0f)
        {
            return Persona.Neutral;
        }

        var neutral = Persona.Neutral;
        var capped = MathF.Min(1f, spread);
        var (bandMinMs, bandMaxMs) = HumanTiming.ReactionBand(level);
        var reactionScale = Range(MinReactionScale, MaxReactionScale);
        var gate = RollGate(spread, out var gateSeconds);
        var wander = HumanTiming.Maybe(DirectRunnerShare)
            ? Range(DirectRunnerMinWander, DirectRunnerMaxWander)
            : Range(WandererMinWander, WandererMaxWander);

        return new Persona(
            ReactionMinMs: (int)(bandMinMs * reactionScale),
            ReactionMaxMs: (int)(bandMaxMs * reactionScale),
            RouteWander: MathF.Min(1f, wander * spread),
            CornerRounding: Range(MinCornerRounding, MaxCornerRounding) * spread,
            MountDistanceScale: Blend(neutral.MountDistanceScale, Range(MinMountDistanceScale, MaxMountDistanceScale), capped),
            DismountNearYalms: Blend(neutral.DismountNearYalms, Range(MinDismountNearYalms, MaxDismountNearYalms), capped),
            Gate: gate,
            GateSeconds: gateSeconds,
            GateLateralYalms: Signed(MinGateLateralYalms, MaxGateLateralYalms) * spread,
            FormationLateralYalms: Signed(MinFormationLateralYalms, MaxFormationLateralYalms) * spread,
            FormationDepthYalms: Range(-MaxFormationDepthYalms, MaxFormationDepthYalms) * spread,
            LeadScale: Blend(neutral.LeadScale, Range(MinLeadScale, MaxLeadScale), capped),
            Restlessness: Range(MinRestlessness, MaxRestlessness) * capped,
            HopChance: HumanTiming.Maybe(HopperShare) ? Range(MinHopChance, MaxHopChance) * capped : 0f,
            JumpsLedges: HumanTiming.Maybe(LedgeJumperShare));
    }

    private static float Spread(HumanizeLevel level) => level switch
    {
        HumanizeLevel.Light => LightSpread,
        HumanizeLevel.Realistic => RealisticSpread,
        HumanizeLevel.Heavy => HeavySpread,
        _ => 0f,
    };

    private static GateStyle RollGate(float spread, out float seconds)
    {
        var roll = HumanTiming.SharedRandom.NextDouble();
        if (roll < EagerShare)
        {
            seconds = Blend(Persona.Neutral.GateSeconds, Range(EagerMinSeconds, EagerMaxSeconds), MathF.Min(1f, spread));
            return GateStyle.Eager;
        }
        if (roll < EagerShare + LateShare)
        {
            seconds = Range(LateMinSeconds, LateMaxSeconds) * spread;
            return GateStyle.Late;
        }

        seconds = Range(OnTimeMinSeconds, OnTimeMaxSeconds);
        return GateStyle.OnTime;
    }

    private static float Range(float min, float max) => min + (float)HumanTiming.SharedRandom.NextDouble() * (max - min);

    private static float Signed(float min, float max) => HumanTiming.Maybe(0.5) ? Range(min, max) : -Range(min, max);

    private static float Blend(float from, float to, float amount) => from + (to - from) * amount;
}
