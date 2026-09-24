using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace AutoPvpSeriesGrind.Core.Game;

internal static unsafe class WalkPace
{
    private static bool engaged;

    public static void Apply(bool walk)
    {
        if (walk == engaged)
        {
            return;
        }

        var control = Control.Instance();
        if (control == null)
        {
            return;
        }

        if (walk && control->IsWalking)
        {
            return;
        }

        control->IsWalking = walk;
        engaged = walk;
    }

    public static void Release() => Apply(false);
}
