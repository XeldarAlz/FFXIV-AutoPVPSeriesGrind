namespace AutoPvpSeriesGrind.Core.Tasks;

// Immutable snapshot of the configuration a run is started with. Captured once at run start so the
// run's behavior can't shift mid-flight if the user edits settings, and so there is a single source
// of truth rather than a fan of mirror fields.
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
    HumanizeLevel Humanize,
    int LeaveDutyDelayMs,
    int RequeueMinSec,
    int RequeueMaxSec,
    bool TakeBreaks,
    int BreakEvery,
    int BreakMinutes)
{
    public static RunSettings From(Configuration cfg) => new(
        SendHello: cfg.SendHelloOnEntry,
        SendGoodMatch: cfg.SendGoodMatchOnResults,
        HelloChance: cfg.HelloChancePercent / 100.0,
        GoodMatchChance: cfg.GoodMatchChancePercent / 100.0,
        HelloDelayMinSec: cfg.HelloDelayMinSeconds,
        HelloDelayMaxSec: cfg.HelloDelayMaxSeconds,
        GoodMatchDelayMinSec: cfg.GoodMatchDelayMinSeconds,
        GoodMatchDelayMaxSec: cfg.GoodMatchDelayMaxSeconds,
        RandomEmotes: cfg.RandomEmotes,
        EnableBrain: cfg.EnableCombatBrain,
        Humanize: cfg.Humanize,
        LeaveDutyDelayMs: Math.Max(0, cfg.LeaveDutyDelaySeconds) * 1000,
        RequeueMinSec: cfg.RequeueDelayMinSeconds,
        RequeueMaxSec: cfg.RequeueDelayMaxSeconds,
        TakeBreaks: cfg.TakeBreaks,
        BreakEvery: cfg.BreakEveryMatches,
        BreakMinutes: cfg.BreakMinutes);
}
