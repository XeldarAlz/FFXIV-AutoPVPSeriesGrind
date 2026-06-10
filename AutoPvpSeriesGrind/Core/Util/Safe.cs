namespace AutoPvpSeriesGrind.Core.Util;

internal static class Safe
{
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

    public static T TrySilent<T>(Func<T> body, T fallback)
    {
        try
        {
            return body();
        }
        catch
        {
            return fallback;
        }
    }
}
