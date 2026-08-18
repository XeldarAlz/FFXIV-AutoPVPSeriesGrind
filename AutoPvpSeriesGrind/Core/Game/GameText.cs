using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace AutoPvpSeriesGrind.Core.Game;

// Quick chat entries are localized: row 1 renders as "Hello" on English clients and "Hallo" on German
// ones. Sending the English literal to a non-English client is a valid command with an unknown argument,
// which the game rejects with an error line, so the row id is resolved through the client's own sheet.
internal static class GameText
{
    private const uint QuickChatHelloRowId = 1;
    private const uint QuickChatGoodMatchRowId = 2;

    private const string QuickChatHelloEnglish = "Hello";
    private const string QuickChatGoodMatchEnglish = "Good Match";

    public static string QuickChatHello()
        => QuickChatCommand(QuickChatHelloRowId, QuickChatHelloEnglish);

    public static string QuickChatGoodMatch()
        => QuickChatCommand(QuickChatGoodMatchRowId, QuickChatGoodMatchEnglish);

    private static string QuickChatCommand(uint rowId, string englishFallback)
    {
        var name = LocalizedQuickChatName(rowId) ?? englishFallback;
        return ContainsWhitespace(name) ? $"/quickchat \"{name}\"" : $"/quickchat {name}";
    }

    private static string? LocalizedQuickChatName(uint rowId)
    {
        try
        {
            var name = Svc.Data.GetExcelSheet<QuickChat>()?.GetRowOrDefault(rowId)?.NameAction.ExtractText();
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch (Exception exception)
        {
            ApsgLog.Warn($"quick chat row {rowId} lookup failed, falling back to English: {exception.Message}");
            return null;
        }
    }

    private static bool ContainsWhitespace(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                return true;
            }
        }
        return false;
    }
}
