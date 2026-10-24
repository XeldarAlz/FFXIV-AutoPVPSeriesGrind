using AutoPvpSeriesGrind.Core.Localization;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal static class LbFeedback
{
    public static void OnFire(LimitBreakOptions settings, string actionName, string targetName)
    {
        if (settings.PlaySoundOnFire)
        {
            PlaySound(settings.FireSoundId);
        }

        if (settings.LogFireToChat)
        {
            ApsgChat.Print(Loc.T(L.LimitBreak.ChatFired, actionName, targetName));
        }
    }

    public static unsafe void PlaySound(int soundId) => UIGlobals.PlayChatSoundEffect((uint)soundId);
}
