using ECommons.DalamudServices;
using ECommons.ExcelServices;
using System.Text;
using GameAction = Lumina.Excel.Sheets.Action;

namespace AutoPvpSeriesGrind.Core.Rotation;

internal static class PvpActionCatalog
{
    private const uint SpellCategory = 2;
    private const uint WeaponskillCategory = 3;
    private const uint AbilityCategory = 4;
    private const uint AllJobsCategory = 1;
    private const ushort GcdRecast100ms = 25;
    private const int MinHotbarSizedKit = 4;
    private const byte DefaultGcdCooldownGroup = 58;

    private static readonly Dictionary<uint, PvpJobKit> Kits = [];

    public static PvpJobKit For(Job job)
    {
        var jobId = (uint)job;
        if (Kits.TryGetValue(jobId, out var cached))
        {
            return cached;
        }

        var kit = Build(job);
        Kits[jobId] = kit;
        LogKit(job, kit);
        return kit;
    }

    private static PvpJobKit Build(Job job)
    {
        var candidates = new List<GameAction>();
        foreach (var action in Svc.Data.GetExcelSheet<GameAction>())
        {
            if (IsJobPvpAction(action, job))
            {
                candidates.Add(action);
            }
        }

        var hotbar = KeepHotbarActions(candidates);
        var abilities = new List<PvpActionInfo>();
        var cooldownGcds = new List<PvpActionInfo>();
        var fillerGcds = new List<PvpActionInfo>();
        for (var actionIndex = 0; actionIndex < hotbar.Count; actionIndex++)
        {
            var info = ToInfo(hotbar[actionIndex]);
            switch (info.Slot)
            {
                case PvpActionSlot.Ability:
                    abilities.Add(info);
                    break;
                case PvpActionSlot.CooldownGcd:
                    cooldownGcds.Add(info);
                    break;
                default:
                    fillerGcds.Add(info);
                    break;
            }
        }

        return new PvpJobKit
        {
            JobId = (uint)job,
            Abilities = abilities.ToArray(),
            CooldownGcds = cooldownGcds.ToArray(),
            FillerGcds = fillerGcds.ToArray(),
            GcdCooldownGroup = fillerGcds.Count > 0 ? fillerGcds[0].CooldownGroup : DefaultGcdCooldownGroup,
        };
    }

    private static bool IsJobPvpAction(in GameAction action, Job job)
    {
        if (!action.IsPvP)
        {
            return false;
        }

        var category = action.ActionCategory.RowId;
        if (category is not (SpellCategory or WeaponskillCategory or AbilityCategory))
        {
            return false;
        }

        if (Array.IndexOf(PvpActions.Shared, action.RowId) >= 0)
        {
            return false;
        }

        var jobCategory = action.ClassJobCategory;
        if (!jobCategory.IsValid || jobCategory.RowId == AllJobsCategory)
        {
            return false;
        }

        return jobCategory.Value.IsJobInCategory(job);
    }

    private static List<GameAction> KeepHotbarActions(List<GameAction> candidates)
    {
        var placeable = new List<GameAction>();
        var comboRoots = new List<GameAction>();
        for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
        {
            var action = candidates[candidateIndex];
            if (action.IsPlayerAction)
            {
                placeable.Add(action);
            }
            if (action.ActionCombo.RowId == 0)
            {
                comboRoots.Add(action);
            }
        }

        return placeable.Count >= MinHotbarSizedKit ? placeable : comboRoots;
    }

    private static PvpActionInfo ToInfo(in GameAction action)
        => new(
            Id: action.RowId,
            Name: action.Name.ExtractText(),
            Slot: SlotOf(action),
            TargetsHostile: action.CanTargetHostile,
            TargetsAlly: action.CanTargetAlly || action.CanTargetParty,
            TargetsSelf: action.CanTargetSelf,
            TargetArea: action.TargetArea,
            HasCastTime: action.Cast100ms > 0,
            CooldownGroup: action.CooldownGroup);

    private static PvpActionSlot SlotOf(in GameAction action)
    {
        if (action.ActionCategory.RowId == AbilityCategory)
        {
            return PvpActionSlot.Ability;
        }

        var isFiller = action.Recast100ms <= GcdRecast100ms && action.MaxCharges == 0;
        return isFiller ? PvpActionSlot.FillerGcd : PvpActionSlot.CooldownGcd;
    }

    private static void LogKit(Job job, PvpJobKit kit)
    {
        var names = new StringBuilder();
        AppendNames(names, "abilities", kit.Abilities);
        AppendNames(names, "cooldown GCDs", kit.CooldownGcds);
        AppendNames(names, "fillers", kit.FillerGcds);
        ApsgLog.Info($"built-in rotation kit for {job}: {names}");
    }

    private static void AppendNames(StringBuilder builder, string label, PvpActionInfo[] actions)
    {
        builder.Append(label).Append(" [");
        for (var actionIndex = 0; actionIndex < actions.Length; actionIndex++)
        {
            if (actionIndex > 0)
            {
                builder.Append(", ");
            }
            builder.Append(actions[actionIndex].Name);
        }
        builder.Append("] ");
    }
}
