namespace AutoPvpSeriesGrind.Core.Tasks;

internal readonly record struct RunSettings(
    bool SendHello,
    bool SendGoodMatch,
    double HelloChance,
    double GoodMatchChance,
    int HelloDelayMinSec,
    int HelloDelayMaxSec,
    int GoodMatchDelayMinSec,
    int GoodMatchDelayMaxSec,
    bool RandomEmotes,
    bool EnableBrain,
    bool BrainTargets,
    HumanizeLevel Humanize,
    int LeaveDutyDelayMs,
    int RequeueMinSec,
    int RequeueMaxSec,
    bool TakeBreaks,
    int BreakEvery,
    int BreakMinutes)
{
    private const double PercentToFractionDivisor = 100.0;
    private const int MillisecondsPerSecond = 1000;

    public static RunSettings From(Configuration cfg) => new(
        SendHello: cfg.SendHelloOnEntry,
        SendGoodMatch: cfg.SendGoodMatchOnResults,
        HelloChance: cfg.HelloChancePercent / PercentToFractionDivisor,
        GoodMatchChance: cfg.GoodMatchChancePercent / PercentToFractionDivisor,
        HelloDelayMinSec: cfg.HelloDelayMinSeconds,
        HelloDelayMaxSec: cfg.HelloDelayMaxSeconds,
        GoodMatchDelayMinSec: cfg.GoodMatchDelayMinSeconds,
        GoodMatchDelayMaxSec: cfg.GoodMatchDelayMaxSeconds,
        RandomEmotes: cfg.RandomEmotes,
        EnableBrain: cfg.EnableCombatBrain,
        BrainTargets: cfg.EnableCombatBrain && cfg.BrainPicksTargets,
        Humanize: cfg.Humanize,
        LeaveDutyDelayMs: Math.Max(0, cfg.LeaveDutyDelaySeconds) * MillisecondsPerSecond,
        RequeueMinSec: cfg.RequeueDelayMinSeconds,
        RequeueMaxSec: cfg.RequeueDelayMaxSeconds,
        TakeBreaks: cfg.TakeBreaks,
        BreakEvery: cfg.BreakEveryMatches,
        BreakMinutes: cfg.BreakMinutes);
}
