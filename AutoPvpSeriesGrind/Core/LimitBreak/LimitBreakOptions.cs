namespace AutoPvpSeriesGrind.Core.LimitBreak;

[Flags]
public enum DutyMask
{
    None = 0,
    CrystallineConflict = 1 << 0,
    Frontline = 1 << 1,
    RivalWings = 1 << 2,
    Other = 1 << 4,
    All = CrystallineConflict | Frontline | RivalWings | Other,
}

public sealed class LimitBreakOptions
{
    public const int DefaultFireSoundId = 7;

    public bool Enabled { get; set; } = true;

    public Dictionary<uint, LbRule> JobRules { get; set; } = [];

    public bool SkipDoomedTargets { get; set; } = true;
    public bool SkipGuardedTargets { get; set; } = true;
    public bool SkipInvulnerableTargets { get; set; } = true;
    public DutyMask EnabledDuties { get; set; } = DutyMask.All;
    public List<string> NameBlocklist { get; set; } = [];

    public bool PlaySoundOnFire { get; set; } = false;
    public int FireSoundId { get; set; } = DefaultFireSoundId;
    public bool LogFireToChat { get; set; } = false;

    public bool LegacySettingsImported { get; set; } = false;

    public LbRule RuleFor(uint jobId) => JobRules.TryGetValue(jobId, out var rule) ? rule : LbPresets.For(jobId);

    public bool IsCustomized(uint jobId) => JobRules.ContainsKey(jobId);

    public LbRule Customize(uint jobId)
    {
        if (!JobRules.TryGetValue(jobId, out var rule))
        {
            rule = LbPresets.For(jobId).Clone();
            JobRules[jobId] = rule;
        }

        return rule;
    }

    public void ResetToPreset(uint jobId) => JobRules.Remove(jobId);

    public bool IsBlocked(string name)
    {
        for (var index = 0; index < NameBlocklist.Count; index++)
        {
            if (string.Equals(NameBlocklist[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
