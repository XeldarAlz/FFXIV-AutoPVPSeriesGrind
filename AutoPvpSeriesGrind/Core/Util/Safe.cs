using System.Runtime.CompilerServices;

namespace AutoPvpSeriesGrind.Core.Util;

internal static class Safe
{
    public static T Try<T>(string label, Func<T> body, T fallback, [CallerFilePath] string callerFile = "")
    {
        try
        {
            return body();
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, label, callerFile);
            return fallback;
        }
    }

    public static void Try(string label, Action body, [CallerFilePath] string callerFile = "")
    {
        try
        {
            body();
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, label, callerFile);
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
