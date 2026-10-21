namespace AutoPvpSeriesGrind.Core.Tasks;

internal static class ChatCommandBlock
{
    public static string[] Lines(string commandBlock)
    {
        if (string.IsNullOrWhiteSpace(commandBlock))
        {
            return [];
        }

        return commandBlock.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool IsCommand(string line) => line.StartsWith('/');
}
