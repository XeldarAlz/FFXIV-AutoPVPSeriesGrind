using AutoPvpSeriesGrind.Core.Localization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed partial class LocalizationTests
{
    private const string SolutionFileName = "AutoPvpSeriesGrind.sln";
    private const string OneSuffix = ".one";
    private const string OtherSuffix = ".other";
    private const int MinimumExpectedEntries = 500;

    private static readonly string[] Languages = ["en", "de", "es", "fr", "ja", "pt", "ru", "tr", "zh"];

    [GeneratedRegex(@"(?<!\{)\{(\d+)(?:[,:][^}]*)?\}")]
    private static partial Regex PlaceholderPattern();

    public static TheoryData<string> AllLanguages()
    {
        var data = new TheoryData<string>();
        for (var languageIndex = 0; languageIndex < Languages.Length; languageIndex++)
        {
            data.Add(Languages[languageIndex]);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void EveryStringIsTranslatedWithMatchingPlaceholders(string language)
    {
        var catalog = LoadCatalog(language);
        var problems = new List<string>();
        var checkedEntries = 0;
        foreach (var entry in SourceEntries())
        {
            checkedEntries++;
            if (entry.Value is LocString text)
            {
                CheckEntry(catalog, text.Key, Placeholders(text.Source), exact: true, problems);
                continue;
            }

            var plural = (LocPlural)entry.Value!;
            var allowed = Placeholders(plural.OneSource);
            allowed.UnionWith(Placeholders(plural.OtherSource));
            CheckEntry(catalog, plural.KeyBase + OneSuffix, allowed, exact: false, problems);
            CheckEntry(catalog, plural.KeyBase + OtherSuffix, Placeholders(plural.OtherSource), exact: true, problems);
        }

        Assert.True(checkedEntries >= MinimumExpectedEntries, $"only {checkedEntries} source strings found");
        Assert.True(problems.Count == 0, $"{language}: {string.Join("; ", problems)}");
    }

    private static void CheckEntry(Dictionary<string, string> catalog, string key, HashSet<int> expected, bool exact, List<string> problems)
    {
        if (!catalog.TryGetValue(key, out var translated))
        {
            problems.Add($"{key} missing");
            return;
        }

        var used = Placeholders(translated);
        if (!used.IsSubsetOf(expected) || (exact && !used.SetEquals(expected)))
        {
            problems.Add($"{key} uses {{{string.Join(",", used)}}}, source has {{{string.Join(",", expected)}}}");
        }
    }

    private static HashSet<int> Placeholders(string text)
    {
        var indices = new HashSet<int>();
        foreach (Match match in PlaceholderPattern().Matches(text))
        {
            indices.Add(int.Parse(match.Groups[1].Value));
        }
        return indices;
    }

    private static IEnumerable<FieldInfo> SourceFields(Type group)
    {
        foreach (var field in group.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            yield return field;
        }
        foreach (var nested in group.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            foreach (var field in SourceFields(nested))
            {
                yield return field;
            }
        }
    }

    private static IEnumerable<KeyValuePair<string, object?>> SourceEntries()
    {
        foreach (var field in SourceFields(typeof(L)))
        {
            var value = field.GetValue(null);
            switch (value)
            {
                case LocString or LocPlural:
                    yield return new(field.Name, value);
                    break;
                case LocString[] lines:
                    for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        yield return new(field.Name, lines[lineIndex]);
                    }
                    break;
            }
        }
    }

    private static Dictionary<string, string> LoadCatalog(string language)
    {
        var path = Path.Combine(RepositoryRoot(), "AutoPvpSeriesGrind", "Localization", language + ".json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var flat = new Dictionary<string, string>(StringComparer.Ordinal);
        Flatten(document.RootElement, string.Empty, flat);
        return flat;
    }

    private static void Flatten(JsonElement element, string prefix, Dictionary<string, string> flat)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            flat[prefix] = element.GetString() ?? string.Empty;
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            Flatten(property.Value, prefix.Length == 0 ? property.Name : prefix + "." + property.Name, flat);
        }
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? throw new InvalidOperationException("solution root not found");
    }
}
