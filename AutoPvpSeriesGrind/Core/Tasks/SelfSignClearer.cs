using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Util;
using ECommons.Automation;
using ECommons.DalamudServices;
using static AutoPvpSeriesGrind.Core.ApsgConstants;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal struct SelfSignClearer
{
    private const int NoticeMinMs = 900;
    private const int NoticeMaxMs = 2600;
    private const int RetryAfterMs = 5000;

    private long clearAtMs;

    public void Reset() => clearAtMs = 0;

    public void Tick()
    {
        if (Svc.Objects.LocalPlayer is not { } localPlayer || !MarkingOps.HasSign(localPlayer.GameObjectId))
        {
            clearAtMs = 0;
            return;
        }

        var now = Environment.TickCount64;
        if (clearAtMs == 0)
        {
            clearAtMs = now + HumanTiming.Reaction(NoticeMinMs, NoticeMaxMs);
            return;
        }
        if (now < clearAtMs)
        {
            return;
        }

        Chat.ExecuteCommand(GameCommands.ClearSignOnSelf);
        clearAtMs = now + RetryAfterMs;
        ApsgLog.Info("sign on self -> cleared");
    }
}
