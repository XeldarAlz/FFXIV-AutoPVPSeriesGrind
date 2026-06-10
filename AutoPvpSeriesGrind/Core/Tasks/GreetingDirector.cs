using AutoPvpSeriesGrind.Core.Util;
using ECommons.Automation;
using static AutoPvpSeriesGrind.Core.ApsgConstants;
using static AutoPvpSeriesGrind.Core.ApsgConstants.CrystallineConflict;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class GreetingDirector
{
    private const double EmoteChance = 0.35;
    private const int MillisecondsPerSecond = 1000;
    private const int PortraitHelloThresholdFloorSeconds = IntroBandLowerSec + 1;
    private const int PortraitHelloThresholdCeilingSeconds = IntroBandUpperSec - 1;

    private int portraitHelloThreshold;
    private bool portraitHelloSent;
    private bool goodMatchSent;

    public void Reset()
    {
        portraitHelloThreshold = 0;
        portraitHelloSent = false;
        goodMatchSent = false;
    }

    public void PrepareForMatch(in RunSettings settings)
    {
        var helloDelay = HumanTiming.RandomSecondsInclusive(settings.HelloDelayMinSec, settings.HelloDelayMaxSec);
        portraitHelloThreshold = Math.Clamp(IntroBandUpperSec - helloDelay, PortraitHelloThresholdFloorSeconds, PortraitHelloThresholdCeilingSeconds);
        portraitHelloSent = false;
        ApsgLog.Info($"portrait hello threshold set -> {portraitHelloThreshold}s");
    }

    public void TryPortraitGreeting(int timeLeftSeconds, in RunSettings settings)
    {
        var greetMoment = !portraitHelloSent && timeLeftSeconds <= portraitHelloThreshold && timeLeftSeconds > IntroBandLowerSec;
        if (!greetMoment || (!settings.SendHello && !settings.RandomEmotes)) return;

        portraitHelloSent = true;
        if (settings.SendHello && HumanTiming.Maybe(settings.HelloChance))
        {
            Chat.ExecuteCommand(GameCommands.QuickChatHello);
            ApsgLog.Info($"quickchat Hello sent at tLeft={timeLeftSeconds} (threshold={portraitHelloThreshold})");
        }
        if (settings.RandomEmotes && HumanTiming.Maybe(EmoteChance))
        {
            var emote = GameCommands.GreetEmotes[HumanTiming.SharedRandom.Next(GameCommands.GreetEmotes.Length)];
            Chat.ExecuteCommand(emote);
            ApsgLog.Info($"random emote '{emote}' sent at tLeft={timeLeftSeconds}");
        }
    }

    public int? PlanGoodMatchDelayMs(in RunSettings settings)
    {
        if (!settings.SendGoodMatch || goodMatchSent) return null;
        goodMatchSent = true;
        if (!HumanTiming.Maybe(settings.GoodMatchChance)) return null;
        return HumanTiming.RandomSecondsInclusive(settings.GoodMatchDelayMinSec, settings.GoodMatchDelayMaxSec) * MillisecondsPerSecond;
    }
}
