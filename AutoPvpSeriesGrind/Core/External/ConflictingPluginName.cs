namespace AutoPvpSeriesGrind.Core.External;

// Compares letters only and ignores case, so spacing, punctuation or a suffix in a renamed fork still matches.
internal static class ConflictingPluginName
{
    private const string RotationSolverLetters = "rotationsolver";

    public static bool IsRotationSolver(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var matched = 0;
        for (var characterIndex = 0; characterIndex < name.Length && matched < RotationSolverLetters.Length; characterIndex++)
        {
            var character = name[characterIndex];
            if (!char.IsLetter(character))
            {
                continue;
            }
            if (char.ToLowerInvariant(character) != RotationSolverLetters[matched])
            {
                return false;
            }

            matched++;
        }

        return matched == RotationSolverLetters.Length;
    }
}
