using AutoPvpSeriesGrind.Core.Util;
using ECommons.Automation;
using static AutoPvpSeriesGrind.Core.ApsgConstants;
using static AutoPvpSeriesGrind.Core.ApsgConstants.CrystallineConflict;

namespace AutoPvpSeriesGrind.Core.Tasks;

// Handles the "human" social touches: a quick-chat hello (and optional emote) during the portrait
// phase, and a "Good Match" on the results screen. Owns its own once-per-match latches.
internal sealed class GreetingDirector
{
    private const double EmoteChance = 0.35;

    private int portraitHelloThreshold;
    private bool portraitHelloSent;
    private bool goodMatchSent;

    public void Reset()
    {
        portraitHelloThreshold = 0;
        portraitHelloSent = false;
        goodMatchSent = false;
    }

    // Picks the in-band moment (seconds left) at which to greet, offset by the configured hello delay.
    public void PrepareForMatch(in RunSettings s)
    {
        var helloDelay = HumanTiming.RandSecInclusive(s.HelloDelayMinSec, s.HelloDelayMaxSec);
        portraitHelloThreshold = Math.Clamp(IntroBandUpperSec - helloDelay, IntroBandLowerSec + 1, IntroBandUpperSec - 1);
        portraitHelloSent = false;
        ApsgLog.Info($"portrait hello threshold set -> {portraitHelloThreshold}s");
    }

    // During the portrait band, fires the hello/emote once the timer reaches the chosen moment.
    public void TryPortraitGreeting(int tLeft, in RunSettings s)
    {
        var greetMoment = !portraitHelloSent && tLeft <= portraitHelloThreshold && tLeft > IntroBandLowerSec;
        if (!greetMoment || (!s.SendHello && !s.RandomEmotes)) return;

        portraitHelloSent = true;
        if (s.SendHello && HumanTiming.Maybe(s.HelloChance))
        {
            Chat.ExecuteCommand(GameCommands.QuickChatHello);
            ApsgLog.Info($"quickchat Hello sent at tLeft={tLeft} (threshold={portraitHelloThreshold})");
        }
        if (s.RandomEmotes && HumanTiming.Maybe(EmoteChance))
        {
            var emote = GameCommands.GreetEmotes[HumanTiming.Rng.Next(GameCommands.GreetEmotes.Length)];
            Chat.ExecuteCommand(emote);
            ApsgLog.Info($"random emote '{emote}' sent at tLeft={tLeft}");
        }
    }

    // Decides whether/when to say "Good Match" on the results screen. Returns the delay (ms) before
    // sending, or null to skip. The latch flips on the first call regardless of the chance roll, so a
    // single results screen never double-greets. The caller owns the actual wait + send.
    public int? PlanGoodMatchDelayMs(in RunSettings s)
    {
        if (!s.SendGoodMatch || goodMatchSent) return null;
        goodMatchSent = true;
        if (!HumanTiming.Maybe(s.GoodMatchChance)) return null;
        return HumanTiming.RandSecInclusive(s.GoodMatchDelayMinSec, s.GoodMatchDelayMaxSec) * 1000;
    }
}
