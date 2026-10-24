using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using System.Globalization;
using System.Text;

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

    private const uint AddonLeavePartyAndDutyRowId = 108;
    private const uint AddonAbandonDutyRowId = 109;

    private static readonly uint[] LeaveDutyPromptRowIds = [AddonLeavePartyAndDutyRowId, AddonAbandonDutyRowId];
    private static string[]? leaveDutyPrompts;

    public static string QuickChatHello()
        => QuickChatCommand(QuickChatHelloRowId, QuickChatHelloEnglish);

    public static string QuickChatGoodMatch()
        => QuickChatCommand(QuickChatGoodMatchRowId, QuickChatGoodMatchEnglish);

    public static bool IsLeaveDutyPrompt(string prompt)
    {
        var known = leaveDutyPrompts ??= LoadLeaveDutyPrompts();
        var compact = Compact(prompt);
        for (var index = 0; index < known.Length; index++)
        {
            if (string.Equals(known[index], compact, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static string[] LoadLeaveDutyPrompts()
    {
        var prompts = new List<string>(LeaveDutyPromptRowIds.Length);
        for (var index = 0; index < LeaveDutyPromptRowIds.Length; index++)
        {
            if (AddonText(LeaveDutyPromptRowIds[index]) is { } text)
            {
                prompts.Add(Compact(text));
            }
        }
        return prompts.ToArray();
    }

    private static string? AddonText(uint rowId)
    {
        try
        {
            var text = Svc.Data.GetExcelSheet<Addon>()?.GetRowOrDefault(rowId)?.Text.ExtractText();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception exception)
        {
            RunLog.Warning($"addon row {rowId} lookup failed: {exception.Message}");
            return null;
        }
    }

    private static string Compact(string text)
    {
        var builder = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (char.IsWhiteSpace(character) || char.GetUnicodeCategory(character) == UnicodeCategory.Format)
            {
                continue;
            }
            builder.Append(character);
        }
        return builder.ToString();
    }

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
            RunLog.Warning($"quick chat row {rowId} lookup failed, falling back to English: {exception.Message}");
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
