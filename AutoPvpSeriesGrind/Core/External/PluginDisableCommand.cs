namespace AutoPvpSeriesGrind.Core.External;

internal static class PluginDisableCommand
{
    public static string? Create(string? internalName)
    {
        if (string.IsNullOrEmpty(internalName)
            || !internalName.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.'))
            return null;
        return $"/xldisableplugin \"{internalName}\"";
    }
}
