using AutoPvpSeriesGrind.Core.Game;
using ECommons.DalamudServices;
using System.Text;
using LuminaAction = Lumina.Excel.Sheets.Action;
using LuminaClassJob = Lumina.Excel.Sheets.ClassJob;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

internal static class LbCatalog
{
    private const uint LimitBreakCategoryId = 15;

    private static readonly Lazy<Dictionary<uint, uint[]>> ActionsByJobId = new(BuildIndex);
    private static readonly Dictionary<uint, LbTargetingProfile> ProfileCache = [];

    public static uint[] ResolveActionIds(uint classJobId)
        => ActionsByJobId.Value.TryGetValue(classJobId, out var actionIds) ? actionIds : [];

    // The follow-up a used limit break turns into is aimed by its own reach, not the limit break's.
    public static LbTargetingProfile ResolveProfile(uint classJobId)
    {
        var actionIds = ResolveActionIds(classJobId);
        for (var actionIndex = 0; actionIndex < actionIds.Length; actionIndex++)
        {
            var current = ActionOps.Adjusted(actionIds[actionIndex]);
            if (current != actionIds[actionIndex])
            {
                return ProfileFor(current);
            }
        }

        return actionIds.Length > 0 ? ProfileFor(actionIds[0]) : LbTargetingProfile.None;
    }

    public static LbTargetingProfile ProfileFor(uint actionId)
    {
        if (!ProfileCache.TryGetValue(actionId, out var profile))
        {
            profile = LbTargetingProfile.FromAction(actionId);
            ProfileCache[actionId] = profile;
        }

        return profile;
    }

    public static string ActionName(uint actionId)
        => Svc.Data.GetExcelSheet<LuminaAction>().TryGetRow(actionId, out var row) ? row.Name.ToString() : $"action {actionId}";

    // A limit break and every follow-up it turns into (Deathflare, Encore of Light, Flare Star) share one button,
    // so each job maps to the whole chain, primaries first.
    private static Dictionary<uint, uint[]> BuildIndex()
    {
        var sheet = Svc.Data.GetExcelSheet<LuminaAction>();
        var primaries = new Dictionary<uint, List<uint>>();
        var followUpsByParent = new Dictionary<uint, List<uint>>();
        foreach (var action in sheet)
        {
            if (!action.IsPvP)
            {
                continue;
            }

            if (action.ActionCombo.RowId != 0)
            {
                Collect(followUpsByParent, action.ActionCombo.RowId, action.RowId);
            }

            var jobId = action.ClassJob.RowId;
            if (action.ActionCategory.RowId == LimitBreakCategoryId && jobId != 0 && jobId != uint.MaxValue)
            {
                Collect(primaries, jobId, action.RowId);
            }
        }

        var result = new Dictionary<uint, uint[]>(primaries.Count);
        foreach (var (jobId, primaryIds) in primaries)
        {
            result[jobId] = WithFollowUps(primaryIds, followUpsByParent);
        }

        LogResolvedMap(result);
        return result;
    }

    private static void Collect(Dictionary<uint, List<uint>> map, uint key, uint actionId)
    {
        if (!map.TryGetValue(key, out var actionIds))
        {
            actionIds = [];
            map[key] = actionIds;
        }

        actionIds.Add(actionId);
    }

    private static uint[] WithFollowUps(List<uint> primaryIds, Dictionary<uint, List<uint>> followUpsByParent)
    {
        var ordered = new List<uint>(primaryIds);
        var seen = new HashSet<uint>(primaryIds);
        var pending = new Queue<uint>(primaryIds);
        while (pending.TryDequeue(out var parent))
        {
            if (!followUpsByParent.TryGetValue(parent, out var followUps))
            {
                continue;
            }

            for (var followUpIndex = 0; followUpIndex < followUps.Count; followUpIndex++)
            {
                var followUp = followUps[followUpIndex];
                if (seen.Add(followUp))
                {
                    ordered.Add(followUp);
                    pending.Enqueue(followUp);
                }
            }
        }

        return ordered.ToArray();
    }

    private static void LogResolvedMap(Dictionary<uint, uint[]> map)
    {
        if (map.Count == 0)
        {
            RunLog.Warning("limit break: no PvP limit breaks found in the action sheet");
            return;
        }

        RunLog.Info($"limit break: resolved PvP limit breaks for {map.Count} jobs");
        var jobs = Svc.Data.GetExcelSheet<LuminaClassJob>();
        var actions = Svc.Data.GetExcelSheet<LuminaAction>();
        var line = new StringBuilder();
        foreach (var (jobId, actionIds) in map)
        {
            line.Clear();
            line.Append("  ").Append(jobId).Append(' ').Append(jobs.TryGetRow(jobId, out var job) ? job.Abbreviation.ToString() : "?").Append(" -> [");
            for (var actionIndex = 0; actionIndex < actionIds.Length; actionIndex++)
            {
                var actionId = actionIds[actionIndex];
                var found = actions.TryGetRow(actionId, out var action);
                line.Append(actionIndex == 0 ? "" : ", ").Append(actionId).Append(" (").Append(found ? action.Name.ToString() : "?");
                line.Append(found && action.TargetArea ? ", placed)" : ")");
            }

            RunLog.Debug(line.Append(']').ToString());
        }
    }
}
