using AutoPvpSeriesGrind.Core.LimitBreak;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ECommons.DalamudServices;
using System.Numerics;
using LuminaClassJob = Lumina.Excel.Sheets.ClassJob;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class LimitBreakSettings
{
    private const float SegmentWidth = 300f;
    private const float MinimumPercent = 1f;
    private const float MaximumPercent = 99f;
    private const int MinimumAbsolute = 1;
    private const int MaximumAbsolute = 500_000;
    private const int AbsoluteStep = 500;
    private const int MinimumCount = 1;
    private const int MaximumCount = 7;
    private const float MinimumAllyHp = 1f;
    private const float MaximumAllyHp = 100f;
    private const float MinimumRadius = 5f;
    private const float MaximumRadius = 40f;
    private const int FirstSoundEffect = 1;
    private const int LastSoundEffect = 16;
    private const int MaximumNameLength = 64;
    private const float NameInputWidth = 260f;
    // A typical PvP max HP, so an absolute threshold previews at a believable spot on the sample bar.
    private const uint SamplePreviewMaxHp = 75_000u;
    private const float PreviewBarHeight = 12f;

    private static readonly Segmented.Item[] fireModes = new Segmented.Item[3];
    private static readonly Segmented.Item[] thresholdModes = new Segmented.Item[2];

    private static uint cachedJobId;
    private static string cachedJobName = string.Empty;
    private static string draftName = string.Empty;

    public static void Draw(Configuration cfg)
    {
        var options = cfg.LimitBreak;
        DrawMainGroup(cfg, options);
        DrawJobGroup(cfg, options);
        DrawSkipGroup(cfg, options);
        DrawDutyGroup(cfg, options);
        DrawBlocklistGroup(cfg, options);
        DrawNotificationGroup(cfg, options);
    }

    private static void DrawMainGroup(Configuration cfg, LimitBreakOptions options)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.LimitBreak.Group));
        DrawToggleRow(cfg, L.LimitBreak.Enabled, L.LimitBreak.EnabledHelp, "##lb_enabled",
            () => options.Enabled, value => options.Enabled = value);

        var controller = Plugin.Instance.LimitBreak;
        if (!controller.LegacyPluginLoaded)
        {
            return;
        }

        SettingsRow.Note(Loc.T(L.LimitBreak.LegacyLoaded), Styling.AccentAmberSoft);
        var label = Loc.T(L.LimitBreak.LegacyDisable);
        SettingsRow.Draw(Loc.T(L.LimitBreak.LegacyRow), null, PillButton.Width(label, FontAwesomeIcon.PowerOff) / ImGuiHelpers.GlobalScale,
            () =>
            {
                if (PillButton.Draw("##lb_disable_legacy", label, Styling.AccentAmber, PillButton.Emphasis.Tinted, FontAwesomeIcon.PowerOff,
                        height: Layout.ActionPillHeight))
                {
                    controller.DisableLegacyPlugin();
                }
            }, Layout.ActionPillHeight);
    }

    private static void DrawJobGroup(Configuration cfg, LimitBreakOptions options)
    {
        var jobId = Svc.Objects.LocalPlayer?.ClassJob.RowId ?? 0u;
        if (jobId == 0)
        {
            using var empty = SettingsGroup.Begin(Loc.T(L.LimitBreak.JobGroupEmpty));
            SettingsRow.Note(Loc.T(L.LimitBreak.NoJob));
            return;
        }

        var jobName = JobName(jobId);
        using var group = SettingsGroup.Begin(Loc.T(L.LimitBreak.JobGroup, jobName));
        var customized = options.IsCustomized(jobId);
        SettingsRow.Note(customized ? Loc.T(L.LimitBreak.CustomNote) : Loc.T(L.LimitBreak.PresetNote, jobName),
            customized ? Styling.TextSecondary : Styling.AccentBlueSoft);
        if (customized)
        {
            DrawResetRow(cfg, options, jobId);
        }

        var rule = options.RuleFor(jobId);
        DrawModeRow(cfg, options, jobId, rule);
        switch (rule.Mode)
        {
            case LbFireMode.Offensive:
                DrawOffensiveRows(cfg, options, jobId, rule);
                break;
            case LbFireMode.Defensive:
                DrawFloatRow(cfg, L.LimitBreak.AllyHp, L.LimitBreak.AllyHpHelp, "##lb_ally_hp", MinimumAllyHp, MaximumAllyHp, L.LimitBreak.AllyHpFormat,
                    () => rule.AllyHpPercent, value => options.Customize(jobId).AllyHpPercent = value);
                DrawTeamRows(cfg, options, jobId, rule, L.LimitBreak.HurtAllies, L.LimitBreak.HurtAlliesHelp);
                break;
            case LbFireMode.Utility:
                DrawTeamRows(cfg, options, jobId, rule, L.LimitBreak.AlliesNear, L.LimitBreak.AlliesNearHelp);
                break;
        }
    }

    // Lumina keeps job names lower case in the Western clients.
    private static string JobName(uint jobId)
    {
        if (jobId == cachedJobId)
        {
            return cachedJobName;
        }

        var name = Svc.Data.GetExcelSheet<LuminaClassJob>().TryGetRow(jobId, out var job) ? job.Name.ToString() : string.Empty;
        cachedJobId = jobId;
        cachedJobName = name.Length == 0 ? jobId.ToString() : string.Concat(char.ToUpperInvariant(name[0]).ToString(), name.AsSpan(1));
        return cachedJobName;
    }

    private static void DrawResetRow(Configuration cfg, LimitBreakOptions options, uint jobId)
    {
        var label = Loc.T(L.LimitBreak.Reset);
        var width = PillButton.Width(label, FontAwesomeIcon.Undo);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, SettingsGroup.ContentRightEdge - ImGui.GetCursorScreenPos().X - width));
        if (PillButton.Draw("##lb_reset", label, Styling.AccentRose, PillButton.Emphasis.Ghost, FontAwesomeIcon.Undo, height: Layout.ActionPillHeight))
        {
            options.ResetToPreset(jobId);
            cfg.Save();
        }

        Styling.VSpace(4f);
    }

    private static void DrawModeRow(Configuration cfg, LimitBreakOptions options, uint jobId, LbRule rule)
    {
        fireModes[0] = new Segmented.Item(FontAwesomeIcon.Bolt, Loc.T(L.LimitBreak.ModeOffensive));
        fireModes[1] = new Segmented.Item(FontAwesomeIcon.ShieldAlt, Loc.T(L.LimitBreak.ModeDefensive));
        fireModes[2] = new Segmented.Item(FontAwesomeIcon.UsersCog, Loc.T(L.LimitBreak.ModeUtility));
        DrawSegmentedRow(cfg, L.LimitBreak.Mode, L.LimitBreak.ModeHelp, "##lb_mode", fireModes,
            () => (int)rule.Mode, value => options.Customize(jobId).Mode = (LbFireMode)value);
        SettingsRow.Caption(Loc.T(rule.Mode switch
        {
            LbFireMode.Defensive => L.LimitBreak.DefensiveBlurb,
            LbFireMode.Utility => L.LimitBreak.UtilityBlurb,
            _ => L.LimitBreak.OffensiveBlurb,
        }));
    }

    private static void DrawOffensiveRows(Configuration cfg, LimitBreakOptions options, uint jobId, LbRule rule)
    {
        thresholdModes[0] = new Segmented.Item(FontAwesomeIcon.Percent, Loc.T(L.LimitBreak.ModePercent));
        thresholdModes[1] = new Segmented.Item(FontAwesomeIcon.Heart, Loc.T(L.LimitBreak.ModeAbsolute));
        DrawSegmentedRow(cfg, L.LimitBreak.ThresholdType, L.LimitBreak.ThresholdTypeHelp, "##lb_threshold_mode", thresholdModes,
            () => (int)rule.EnemyHpMode, value => options.Customize(jobId).EnemyHpMode = (ThresholdMode)value);

        if (rule.EnemyHpMode == ThresholdMode.Percent)
        {
            DrawFloatRow(cfg, L.LimitBreak.FireBelow, L.LimitBreak.FireBelowPercentHelp, "##lb_threshold_percent", MinimumPercent, MaximumPercent,
                L.LimitBreak.PercentFormat, () => rule.EnemyHpPercent, value => options.Customize(jobId).EnemyHpPercent = value);
        }
        else
        {
            DrawStepperRow(cfg, L.LimitBreak.FireBelow, L.LimitBreak.FireBelowAbsoluteHelp, "##lb_threshold_absolute", AbsoluteStep,
                MinimumAbsolute, MaximumAbsolute, Loc.T(L.LimitBreak.AbsoluteFormat),
                () => (int)Math.Min(rule.EnemyHpAbsolute, MaximumAbsolute), value => options.Customize(jobId).EnemyHpAbsolute = (uint)value);
        }

        DrawPreview(rule);
    }

    private static void DrawTeamRows(Configuration cfg, LimitBreakOptions options, uint jobId, LbRule rule, LocString countLabel, LocString countHelp)
    {
        DrawStepperRow(cfg, countLabel, countHelp, "##lb_ally_count", 1, MinimumCount, MaximumCount, "%d",
            () => rule.AllyCountNear, value => options.Customize(jobId).AllyCountNear = value);
        DrawFloatRow(cfg, L.LimitBreak.AllyRadius, L.LimitBreak.AllyRadiusHelp, "##lb_ally_radius", MinimumRadius, MaximumRadius,
            L.LimitBreak.RangeFormat, () => rule.AllyRadiusYalms, value => options.Customize(jobId).AllyRadiusYalms = value);
        DrawStepperRow(cfg, L.LimitBreak.EnemiesNear, L.LimitBreak.EnemiesNearHelp, "##lb_enemy_count", 1, MinimumCount, MaximumCount, "%d",
            () => rule.EnemyCountNear, value => options.Customize(jobId).EnemyCountNear = value);
        DrawFloatRow(cfg, L.LimitBreak.EnemyRadius, L.LimitBreak.EnemyRadiusHelp, "##lb_enemy_radius", MinimumRadius, MaximumRadius,
            L.LimitBreak.RangeFormat, () => rule.EnemyRadiusYalms, value => options.Customize(jobId).EnemyRadiusYalms = value);
    }

    // A sample HP bar with the fire zone filled, so the value reads as "this much HP left means it fires"
    // rather than as an abstract number.
    private static void DrawPreview(LbRule rule)
    {
        var percentMode = rule.EnemyHpMode == ThresholdMode.Percent;
        var fraction = Math.Clamp(percentMode ? rule.EnemyHpPercent / 100f : (float)rule.EnemyHpAbsolute / SamplePreviewMaxHp, 0.01f, 0.99f);
        var scale = ImGuiHelpers.GlobalScale;

        Styling.VSpace(10f);
        var origin = ImGui.GetCursorScreenPos();
        var width = SettingsGroup.ContentRightEdge - origin.X;
        var height = PreviewBarHeight * scale;
        var shown = Motion.Approach(Motion.Key("##lb_preview"), fraction);
        Paint.Bar(ImGui.GetWindowDrawList(), origin, width, height, shown, Styling.AccentRose);
        ImGui.Dummy(new Vector2(width, height));

        Styling.VSpace(4f);
        using (Fonts.PushCaption())
        {
            var labels = ImGui.GetCursorScreenPos();
            TextDraw.At(Loc.T(L.LimitBreak.PreviewEmpty), labels, Styling.TextMuted);
            TextDraw.Right(Loc.T(L.LimitBreak.PreviewFull), labels.X + width, labels.Y, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, ImGui.GetTextLineHeight()));
        }

        SettingsRow.Note(percentMode
            ? Loc.T(L.LimitBreak.PreviewPercent, (int)MathF.Round(rule.EnemyHpPercent))
            : Loc.T(L.LimitBreak.PreviewAbsolute, rule.EnemyHpAbsolute.ToString("N0", Loc.Culture)), Styling.TextSecondary);
    }

    private static void DrawSkipGroup(Configuration cfg, LimitBreakOptions options)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.LimitBreak.SkipGroup));
        DrawToggleRow(cfg, L.LimitBreak.SkipDoomed, L.LimitBreak.SkipDoomedHelp, "##lb_skip_doomed",
            () => options.SkipDoomedTargets, value => options.SkipDoomedTargets = value);
        DrawToggleRow(cfg, L.LimitBreak.SkipGuarded, L.LimitBreak.SkipGuardedHelp, "##lb_skip_guarded",
            () => options.SkipGuardedTargets, value => options.SkipGuardedTargets = value);
        DrawToggleRow(cfg, L.LimitBreak.SkipInvulnerable, L.LimitBreak.SkipInvulnerableHelp, "##lb_skip_invulnerable",
            () => options.SkipInvulnerableTargets, value => options.SkipInvulnerableTargets = value);
    }

    private static void DrawDutyGroup(Configuration cfg, LimitBreakOptions options)
    {
        using (SettingsGroup.Begin(Loc.T(L.LimitBreak.DutyGroup)))
        {
            DrawDutyRow(cfg, options, DutyMask.CrystallineConflict, L.LimitBreak.DutyCrystallineConflict, "##lb_duty_cc");
            DrawDutyRow(cfg, options, DutyMask.Frontline, L.LimitBreak.DutyFrontline, "##lb_duty_frontline");
            DrawDutyRow(cfg, options, DutyMask.RivalWings, L.LimitBreak.DutyRivalWings, "##lb_duty_rival_wings");
            DrawDutyRow(cfg, options, DutyMask.Other, L.LimitBreak.DutyOther, "##lb_duty_other");
        }

        SettingsGroup.Footnote(Loc.T(L.LimitBreak.DutyFootnote));
    }

    private static void DrawDutyRow(Configuration cfg, LimitBreakOptions options, DutyMask duty, LocString label, string id)
        => SettingsRow.Draw(Loc.T(label), null, SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => (options.EnabledDuties & duty) != 0,
                value => options.EnabledDuties = value ? options.EnabledDuties | duty : options.EnabledDuties & ~duty, id),
            SettingsRow.ToggleHeight);

    private static void DrawBlocklistGroup(Configuration cfg, LimitBreakOptions options)
    {
        var names = options.NameBlocklist;
        using var group = SettingsGroup.Begin(Loc.T(L.LimitBreak.BlocklistGroup));
        SettingsRow.DrawBlock(Loc.T(L.LimitBreak.BlocklistAdd), Loc.T(L.LimitBreak.BlocklistAddHelp), () => DrawNameInput(cfg, options));

        if (names.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.LimitBreak.BlocklistEmpty));
            return;
        }

        var removeIndex = -1;
        var buttonSize = ImGui.GetFrameHeight();
        var buttonUnits = buttonSize / ImGuiHelpers.GlobalScale;
        var removeLabel = Loc.T(L.Common.Remove);
        for (var nameIndex = 0; nameIndex < names.Count; nameIndex++)
        {
            var index = nameIndex;
            ImGui.PushID(index);
            SettingsRow.Draw(names[index], null, buttonUnits, () =>
            {
                if (IconButton.Draw(FontAwesomeIcon.Times, "##lb_blocklist_remove", buttonSize, Styling.AccentRose, removeLabel))
                {
                    removeIndex = index;
                }
            }, buttonUnits);
            ImGui.PopID();
        }

        if (removeIndex < 0)
        {
            return;
        }

        names.RemoveAt(removeIndex);
        cfg.Save();
    }

    private static void DrawNameInput(Configuration cfg, LimitBreakOptions options)
    {
        bool entered;
        using (SettingsControls.PushFrameColors())
        {
            ImGui.SetNextItemWidth(NameInputWidth * ImGuiHelpers.GlobalScale);
            entered = ImGui.InputTextWithHint("##lb_blocklist_input", Loc.T(L.LimitBreak.BlocklistHint), ref draftName, MaximumNameLength,
                ImGuiInputTextFlags.EnterReturnsTrue);
        }

        ImGui.SameLine();
        var name = draftName.Trim();
        var clicked = PillButton.Draw("##lb_blocklist_add", Loc.T(L.Common.Add), Styling.AccentArc, PillButton.Emphasis.Tinted, FontAwesomeIcon.Plus,
            name.Length > 0, ImGui.GetFrameHeight() / ImGuiHelpers.GlobalScale);
        if (!entered && !clicked)
        {
            return;
        }

        draftName = string.Empty;
        if (name.Length == 0 || options.IsBlocked(name))
        {
            return;
        }

        options.NameBlocklist.Add(name);
        cfg.Save();
    }

    private static void DrawNotificationGroup(Configuration cfg, LimitBreakOptions options)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.LimitBreak.NotificationGroup));
        DrawToggleRow(cfg, L.LimitBreak.PlaySound, L.LimitBreak.PlaySoundHelp, "##lb_play_sound",
            () => options.PlaySoundOnFire, value => options.PlaySoundOnFire = value);
        if (options.PlaySoundOnFire)
        {
            DrawStepperRow(cfg, L.LimitBreak.SoundEffect, L.LimitBreak.SoundEffectHelp, "##lb_sound_id", 1, FirstSoundEffect, LastSoundEffect,
                Loc.T(L.LimitBreak.SoundEffectFormat), () => Math.Clamp(options.FireSoundId, FirstSoundEffect, LastSoundEffect),
                value => options.FireSoundId = value);
            var test = Loc.T(L.LimitBreak.Test);
            SettingsRow.Draw(Loc.T(L.LimitBreak.SoundPreview), Loc.T(L.LimitBreak.SoundPreviewHelp),
                PillButton.Width(test, FontAwesomeIcon.Play) / ImGuiHelpers.GlobalScale, () =>
                {
                    if (PillButton.Draw("##lb_sound_test", test, Styling.AccentArc, PillButton.Emphasis.Tinted, FontAwesomeIcon.Play,
                            height: Layout.ActionPillHeight))
                    {
                        LbFeedback.PlaySound(options.FireSoundId);
                    }
                }, Layout.ActionPillHeight);
        }

        DrawToggleRow(cfg, L.LimitBreak.LogToChat, L.LimitBreak.LogToChatHelp, "##lb_log_chat",
            () => options.LogFireToChat, value => options.LogFireToChat = value);
    }

    private static void DrawToggleRow(Configuration cfg, LocString label, LocString help, string id, Func<bool> getter, Action<bool> setter)
        => SettingsRow.Draw(Loc.T(label), Loc.T(help), SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, getter, setter, id), SettingsRow.ToggleHeight);

    private static void DrawFloatRow(Configuration cfg, LocString label, LocString help, string id, float minimum, float maximum, LocString format,
        Func<float> getter, Action<float> setter)
        => SettingsRow.Draw(Loc.T(label), Loc.T(help), SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawFloatSlider(cfg, id, getter, setter, minimum, maximum, Loc.T(format)));

    private static void DrawStepperRow(Configuration cfg, LocString label, LocString help, string id, int step, int minimum, int maximum,
        string format, Func<int> getter, Action<int> setter)
        => SettingsRow.Draw(Loc.T(label), Loc.T(help), Stepper.DefaultWidth, () =>
        {
            var value = getter();
            if (Stepper.Draw(id, ref value, step, minimum, maximum, format))
            {
                setter(value);
                cfg.SaveDebounced();
            }
        });

    private static void DrawSegmentedRow(Configuration cfg, LocString label, LocString help, string id, Segmented.Item[] items,
        Func<int> getter, Action<int> setter)
    {
        var height = Layout.SegmentHeight * 0.8f;
        SettingsRow.Draw(Loc.T(label), Loc.T(help), SegmentWidth, () =>
        {
            var selected = getter();
            if (Segmented.Draw(id, items, ref selected, true, height, SegmentWidth * ImGuiHelpers.GlobalScale) && selected != getter())
            {
                setter(selected);
                cfg.Save();
            }
        }, height);
    }
}
