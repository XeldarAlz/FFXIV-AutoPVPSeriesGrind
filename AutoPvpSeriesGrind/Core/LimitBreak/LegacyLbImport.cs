using Newtonsoft.Json.Linq;
using System.IO;

namespace AutoPvpSeriesGrind.Core.LimitBreak;

// Carries a player's own Auto PVP LB settings over once, the first time this plugin runs with the limit break
// built in. Rules the old plugin received as presets are skipped: the built-in presets replace them.
internal static class LegacyLbImport
{
    private const string LegacyConfigFileName = LimitBreakController.LegacyPluginInternalName + ".json";
    private const string UserRuleSource = "User";

    public static void RunOnce(Configuration configuration)
    {
        var settings = configuration.LimitBreak;
        if (settings.LegacySettingsImported)
        {
            return;
        }

        settings.LegacySettingsImported = true;
        var path = Path.Combine(Plugin.PluginInterface.ConfigFile.DirectoryName ?? string.Empty, LegacyConfigFileName);
        if (File.Exists(path))
        {
            TryImport(path, settings);
        }

        configuration.Save();
    }

    private static void TryImport(string path, LimitBreakOptions settings)
    {
        try
        {
            var legacy = JObject.Parse(File.ReadAllText(path));
            settings.Enabled = legacy.Value<bool?>("Enabled") ?? settings.Enabled;
            settings.SkipDoomedTargets = legacy.Value<bool?>("SkipDoomedTargets") ?? settings.SkipDoomedTargets;
            settings.SkipGuardedTargets = legacy.Value<bool?>("SkipGuardedTargets") ?? settings.SkipGuardedTargets;
            settings.SkipInvulnerableTargets = legacy.Value<bool?>("SkipInvulnerableTargets") ?? settings.SkipInvulnerableTargets;
            settings.PlaySoundOnFire = legacy.Value<bool?>("PlaySoundOnFire") ?? settings.PlaySoundOnFire;
            settings.FireSoundId = legacy.Value<int?>("FireSoundId") ?? settings.FireSoundId;
            settings.LogFireToChat = legacy.Value<bool?>("LogFireToChat") ?? settings.LogFireToChat;
            if (legacy.Value<int?>("EnabledDuties") is { } duties)
            {
                settings.EnabledDuties = (DutyMask)duties & DutyMask.All;
            }

            ImportBlocklist(legacy["NameBlocklist"] as JArray, settings.NameBlocklist);
            var rules = ImportUserRules(legacy["PerJobRules"] as JObject, settings.JobRules);
            RunLog.Info($"limit break: imported Auto PVP LB settings ({rules} custom job rules, {settings.NameBlocklist.Count} blocked names)");
        }
        catch (Exception exception)
        {
            RunLog.Warning(exception, "limit break: could not import the Auto PVP LB settings; starting from the defaults");
        }
    }

    private static void ImportBlocklist(JArray? names, List<string> blocklist)
    {
        if (names is null)
        {
            return;
        }

        for (var nameIndex = 0; nameIndex < names.Count; nameIndex++)
        {
            if (names[nameIndex].Value<string>() is { Length: > 0 } name)
            {
                blocklist.Add(name);
            }
        }
    }

    private static int ImportUserRules(JObject? rules, Dictionary<uint, LbRule> jobRules)
    {
        if (rules is null)
        {
            return 0;
        }

        var imported = 0;
        foreach (var (key, token) in rules)
        {
            if (!uint.TryParse(key, out var jobId) || token is not JObject rule || rule.Value<string>("Source") != UserRuleSource)
            {
                continue;
            }

            jobRules[jobId] = ParseRule(rule);
            imported++;
        }

        return imported;
    }

    private static LbRule ParseRule(JObject rule)
    {
        var parsed = new LbRule();
        parsed.Mode = Enum.TryParse<LbFireMode>(rule.Value<string>("Mode"), out var mode) ? mode : parsed.Mode;
        parsed.EnemyHpMode = Enum.TryParse<ThresholdMode>(rule.Value<string>("EnemyHpMode"), out var hpMode) ? hpMode : parsed.EnemyHpMode;
        parsed.EnemyHpPercent = rule.Value<float?>("EnemyHpPercent") ?? parsed.EnemyHpPercent;
        parsed.EnemyHpAbsolute = rule.Value<uint?>("EnemyHpAbsolute") ?? parsed.EnemyHpAbsolute;
        parsed.AllyHpPercent = rule.Value<float?>("AllyHpPercent") ?? parsed.AllyHpPercent;
        parsed.AllyCountNear = rule.Value<int?>("AllyCountNear") ?? parsed.AllyCountNear;
        parsed.AllyRadiusYalms = rule.Value<float?>("AllyRadiusYalms") ?? parsed.AllyRadiusYalms;
        parsed.EnemyCountNear = rule.Value<int?>("EnemyCountNear") ?? parsed.EnemyCountNear;
        parsed.EnemyRadiusYalms = rule.Value<float?>("EnemyRadiusYalms") ?? parsed.EnemyRadiusYalms;
        return parsed;
    }
}
