namespace AutoPvpSeriesGrind.Core.Game;

// JobId -> PvP Limit Break action name(s), ported verbatim from the source script's LIMIT_BREAK_BY_JOB
// table. Some jobs list two names (the live action depends on stance/form), so both are fired in turn.
internal static class LimitBreakCatalog
{
    private static readonly IReadOnlyDictionary<uint, string[]> ByJob = new Dictionary<uint, string[]>
    {
        [19] = ["Phalanx"],                              // PLD
        [21] = ["Primal Scream"],                        // WAR
        [32] = ["Eventide"],                             // DRK
        [37] = ["Relentless Rush", "Terminal Trigger"],  // GNB
        [24] = ["Afflatus Purgation"],                   // WHM
        [33] = ["Celestial River"],                      // AST
        [40] = ["Mesotes"],                              // SGE
        [23] = ["Final Fantasia"],                       // BRD
        [31] = ["Marksman's Spite"],                     // MCH
        [38] = ["Contradance"],                          // DNC
        [20] = ["Meteodrive"],                           // MNK
        [22] = ["Sky High", "Sky Shatter"],              // DRG
        [30] = ["Seiton Tenchu"],                        // NIN
        [34] = ["Zantetsuken"],                          // SAM
        [39] = ["Tenebrae Lemurum"],                     // RPR
        [25] = ["Soul Resonance"],                       // BLM
        [27] = ["Summon Bahamut", "Summon Phoenix"],     // SMN
        [41] = ["World-swallower"],                      // VPR
        [42] = ["Advent of Chocobastion"],               // PCT
        [35] = ["Southern Cross"],                       // RDM
        [28] = ["Seraphism"],                            // SCH
    };

    public static IReadOnlyList<string> NamesForJob(uint jobId)
        => ByJob.TryGetValue(jobId, out var names) ? names : [];
}
