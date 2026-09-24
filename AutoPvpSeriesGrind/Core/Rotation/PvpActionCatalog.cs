using ECommons.DalamudServices;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using System.Text;
using GameAction = Lumina.Excel.Sheets.Action;

namespace AutoPvpSeriesGrind.Core.Rotation;

internal static class PvpActionCatalog
{
    private const uint SpellCategory = 2;
    private const uint WeaponskillCategory = 3;
    private const uint AbilityCategory = 4;
    private const uint AllJobsCategory = 1;
    private const uint DisciplesOfWarOrMagicCategory = 85;
    private const ushort GcdRecast100ms = 25;
    private const byte DefaultGcdCooldownGroup = 58;

    private readonly record struct IndexedAction(PvpActionInfo Info, uint JobCategoryRow, bool Placeable);

    private static readonly (uint JobId, uint Starter)[] ComboStarters =
    [
        (19, 29058), (20, 29475), (21, 29074), (22, 29486), (30, 29500), (32, 29085),
        (34, 29523), (37, 29098), (38, 29416), (39, 29538), (41, 39157),
    ];

    private static readonly Dictionary<uint, IndexedAction> Index = [];
    private static readonly Dictionary<uint, PvpJobKit> Kits = [];
    private static bool indexBuilt;

    public static bool TryGetInfo(uint actionId, out PvpActionInfo info)
    {
        EnsureIndex();
        if (Index.TryGetValue(actionId, out var entry))
        {
            info = entry.Info;
            return true;
        }

        info = default;
        return false;
    }

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

    private static void EnsureIndex()
    {
        if (indexBuilt)
        {
            return;
        }

        foreach (var action in Svc.Data.GetExcelSheet<GameAction>())
        {
            if (!action.IsPvP)
            {
                continue;
            }
            var category = action.ActionCategory.RowId;
            if (category is not (SpellCategory or WeaponskillCategory or AbilityCategory))
            {
                continue;
            }
            Index[action.RowId] = new IndexedAction(ToInfo(action), action.ClassJobCategory.RowId, action.IsPlayerAction);
        }

        indexBuilt = true;
    }

    private static PvpJobKit Build(Job job)
    {
        EnsureIndex();
        var jobCategories = Svc.Data.GetExcelSheet<ClassJobCategory>();
        var buttons = new List<PvpActionInfo>();
        foreach (var (actionId, entry) in Index)
        {
            if (!entry.Placeable || Array.IndexOf(PvpActions.Shared, actionId) >= 0)
            {
                continue;
            }
            if (entry.JobCategoryRow is AllJobsCategory or DisciplesOfWarOrMagicCategory)
            {
                continue;
            }
            if (!jobCategories.TryGetRow(entry.JobCategoryRow, out var jobCategory) || !jobCategory.IsJobInCategory(job))
            {
                continue;
            }
            buttons.Add(entry.Info);
        }

        AddComboStarter(job, buttons);
        var ordered = buttons.ToArray();
        Array.Sort(ordered, static (left, right) => left.Slot != right.Slot ? left.Slot.CompareTo(right.Slot) : left.Id.CompareTo(right.Id));

        return new PvpJobKit
        {
            JobId = (uint)job,
            Buttons = ordered,
            GcdCooldownGroup = FillerCooldownGroup(ordered),
        };
    }

    private static void AddComboStarter(Job job, List<PvpActionInfo> buttons)
    {
        for (var starterIndex = 0; starterIndex < ComboStarters.Length; starterIndex++)
        {
            var (jobId, starter) = ComboStarters[starterIndex];
            if (jobId != (uint)job)
            {
                continue;
            }
            if (Index.TryGetValue(starter, out var entry))
            {
                buttons.Add(entry.Info);
            }
            return;
        }
    }

    private static byte FillerCooldownGroup(PvpActionInfo[] buttons)
    {
        for (var buttonIndex = 0; buttonIndex < buttons.Length; buttonIndex++)
        {
            if (buttons[buttonIndex].Slot == PvpActionSlot.FillerGcd)
            {
                return buttons[buttonIndex].CooldownGroup;
            }
        }
        return DefaultGcdCooldownGroup;
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
            Range: action.Range,
            EffectRange: action.EffectRange,
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
        for (var buttonIndex = 0; buttonIndex < kit.Buttons.Length; buttonIndex++)
        {
            if (buttonIndex > 0)
            {
                names.Append(", ");
            }
            names.Append(kit.Buttons[buttonIndex].Name);
        }
        var tableState = JobRotationTables.TryGet((uint)job, out _) ? "with a job table" : "generic order";
        RunLog.Info($"built-in rotation kit for {job} ({tableState}): {names}");
    }
}
