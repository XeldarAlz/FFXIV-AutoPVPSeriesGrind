using AutoPvpSeriesGrind.Core.Combat;
using Dalamud.Interface;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows;

internal static class PostureStyle
{
    public static Vector4 Color(Posture posture) => posture switch
    {
        Posture.Retreat => Styling.AccentRose,
        Posture.Regroup => Styling.AccentAmberSoft,
        Posture.Reposition => Styling.AccentAmber,
        Posture.Stage => Styling.AccentVioletSoft,
        Posture.Push => Styling.AccentAmber,
        Posture.Hold => Styling.AccentMint,
        _ => Styling.TextMuted,
    };

    public static FontAwesomeIcon Icon(Posture posture) => posture switch
    {
        Posture.Retreat => FontAwesomeIcon.Running,
        Posture.Regroup => FontAwesomeIcon.Users,
        Posture.Reposition => FontAwesomeIcon.Walking,
        Posture.Stage => FontAwesomeIcon.HourglassHalf,
        Posture.Push => FontAwesomeIcon.Crosshairs,
        _ => FontAwesomeIcon.ShieldAlt,
    };
}
