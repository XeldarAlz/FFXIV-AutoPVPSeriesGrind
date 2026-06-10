using AutoPvpSeriesGrind.Core.Util;
using ECommons.Automation;
using ECommons.GameHelpers;
using static AutoPvpSeriesGrind.Core.ApsgConstants;
using static AutoPvpSeriesGrind.Core.ApsgConstants.CrystallineConflict;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class GreetingDirector
{
    private const double SecondEmoteChance = 0.35;
    private const int MillisecondsPerSecond = 1000;
    private const int PortraitHelloThresholdFloorSeconds = IntroBandLowerSec + 1;
    private const int PortraitHelloThresholdCeilingSeconds = IntroBandUpperSec - 1;
    private const int FirstEmoteEarliestSec = 26;
    private const int FirstEmoteLatestSec = 12;
    private const int SecondEmoteEarliestSec = 10;
    private const int SecondEmoteLatestSec = 4;

    private readonly Queue<int> emoteMoments = new();
    private int portraitHelloThreshold;
    private bool portraitHelloSent;
    private bool goodMatchSent;
    private bool emoteDeferLogged;

    public void Reset()
    {
        portraitHelloThreshold = 0;
        portraitHelloSent = false;
        goodMatchSent = false;
        emoteMoments.Clear();
        emoteDeferLogged = false;
    }

    public void PrepareForMatch(in RunSettings settings)
    {
        var helloDelay = HumanTiming.RandomSecondsInclusive(settings.HelloDelayMinSec, settings.HelloDelayMaxSec);
        portraitHelloThreshold = Math.Clamp(IntroBandUpperSec - helloDelay, PortraitHelloThresholdFloorSeconds, PortraitHelloThresholdCeilingSeconds);
        portraitHelloSent = false;
        ApsgLog.Info($"portrait hello threshold set -> {portraitHelloThreshold}s");

        emoteMoments.Clear();
        emoteDeferLogged = false;
        if (settings.RandomEmotes)
        {
            emoteMoments.Enqueue(HumanTiming.RandomSecondsInclusive(FirstEmoteLatestSec, FirstEmoteEarliestSec));
            if (HumanTiming.Maybe(SecondEmoteChance))
            {
                emoteMoments.Enqueue(HumanTiming.RandomSecondsInclusive(SecondEmoteLatestSec, SecondEmoteEarliestSec));
            }
            ApsgLog.Info($"emote moments planned -> [{string.Join(", ", emoteMoments)}]s left");
        }
    }

    public void TickIntro(int timeLeftSeconds, in RunSettings settings)
    {
        // Never send an emote on the same tick as the hello quickchat; the second command can get eaten.
        if (TryHello(timeLeftSeconds, settings)) return;
        TryEmote(timeLeftSeconds, settings);
    }

    private bool TryHello(int timeLeftSeconds, in RunSettings settings)
    {
        var greetMoment = !portraitHelloSent && timeLeftSeconds <= portraitHelloThreshold && timeLeftSeconds > IntroBandLowerSec;
        if (!greetMoment || !settings.SendHello) return false;

        portraitHelloSent = true;
        if (!HumanTiming.Maybe(settings.HelloChance)) return false;

        Chat.ExecuteCommand(GameCommands.QuickChatHello);
        ApsgLog.Info($"quickchat Hello sent at tLeft={timeLeftSeconds} (threshold={portraitHelloThreshold})");
        return true;
    }

    private void TryEmote(int timeLeftSeconds, in RunSettings settings)
    {
        if (!settings.RandomEmotes || emoteMoments.Count == 0) return;
        if (timeLeftSeconds <= IntroBandLowerSec)
        {
            emoteMoments.Clear();
            return;
        }
        if (timeLeftSeconds > emoteMoments.Peek()) return;

        // The game silently drops emotes while the character is loading in, occupied, moving,
        // jumping, or animation-locked; keep the moment pending and retry next tick.
        if (!CanEmoteNow())
        {
            if (!emoteDeferLogged)
            {
                ApsgLog.Info($"emote deferred at tLeft={timeLeftSeconds} (player busy/moving) -> retrying");
                emoteDeferLogged = true;
            }
            return;
        }

        emoteMoments.Dequeue();
        emoteDeferLogged = false;
        var emote = GameCommands.GreetEmotes[HumanTiming.SharedRandom.Next(GameCommands.GreetEmotes.Length)];
        Chat.ExecuteCommand(emote);
        ApsgLog.Info($"random emote '{emote}' sent at tLeft={timeLeftSeconds}");
    }

    private static bool CanEmoteNow() => Player.Available && !Player.IsDead && !Player.IsBusy;

    public int? PlanGoodMatchDelayMs(in RunSettings settings)
    {
        if (!settings.SendGoodMatch || goodMatchSent) return null;
        goodMatchSent = true;
        if (!HumanTiming.Maybe(settings.GoodMatchChance)) return null;
        return HumanTiming.RandomSecondsInclusive(settings.GoodMatchDelayMinSec, settings.GoodMatchDelayMaxSec) * MillisecondsPerSecond;
    }
}
