using ECommons.Throttlers;

namespace AutoPvpSeriesGrind.Core.Util;

internal static class Safe
{
    private const int NoteThrottleMs = 30_000;

    public static T Try<T>(string label, Func<T> body, T fallback)
    {
        try
        {
            return body();
        }
        catch (Exception exception)
        {
            ApsgLog.Warn(exception, label);
            return fallback;
        }
    }

    public static void Try(string label, Action body)
    {
        try
        {
            body();
        }
        catch (Exception exception)
        {
            ApsgLog.Warn(exception, label);
        }
    }

    public static T TrySilent<T>(string label, Func<T> body, T fallback)
    {
        try
        {
            return body();
        }
        catch (Exception exception)
        {
            Note(label, exception);
            return fallback;
        }
    }

    public static void Note(string label, Exception exception)
    {
        if (EzThrottler.Throttle($"ApsgSafeNote.{label}", NoteThrottleMs))
        {
            ApsgLog.Debug($"{label}: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
