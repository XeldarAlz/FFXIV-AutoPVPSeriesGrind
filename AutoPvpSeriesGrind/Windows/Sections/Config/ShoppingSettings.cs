using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Shopping;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections.Config;

internal static class ShoppingSettings
{
    private const int ThresholdMinimum = 2_000;
    private const int RefreshMs = 1_000;
    private const float PickerWidth = 380f;
    private const float LeftoverComboWidth = 260f;
    private const float StopControlWidth = 150f;
    private const float PillHeight = 28f;

    private readonly record struct ListRow(uint ItemId, string Name, Vector4 NameColor, bool IsCollectible, string Status, Vector4 StatusColor);

    private static readonly HashSet<uint> noSkippedItems = [];

    private static LanguageInfo? labelsLanguage;
    private static string[] pickerLabels = [];
    private static uint[] leftoverItemIds = [];
    private static string[] leftoverLabels = [];
    private static int pickerSelection;
    private static int leftoverSelection;

    private static ListRow[] listRows = [];
    private static long refreshedAtMs;
    private static int heldMarks;
    private static int heldCrystals;
    private static bool hasSomethingToBuy;

    public static void Draw(Configuration cfg)
    {
        EnsureLabels();
        Refresh(cfg);
        DrawWhenGroup(cfg);
        DrawListGroup(cfg);
        DrawLeftoverGroup(cfg);
        SettingsGroup.Footnote(Loc.T(L.Shop.Footnote));
    }

    private static void DrawWhenGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Shop.GroupWhen));

        SettingsRow.Draw(Loc.T(L.Shop.BetweenMatches),
            Loc.T(L.Shop.BetweenMatchesHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.ShopBetweenMatches, value => cfg.ShopBetweenMatches = value, "##shop_between"),
            SettingsRow.ToggleHeight);

        if (cfg.ShopBetweenMatches)
        {
            SettingsRow.Draw(Loc.T(L.Shop.Threshold),
                Loc.T(L.Shop.ThresholdHelp),
                SettingsControls.RowSliderWidth,
                () => SettingsControls.DrawIntSlider(cfg, "##shop_threshold",
                    () => cfg.ShopMarksThreshold, value => cfg.ShopMarksThreshold = value,
                    ThresholdMinimum, PvpWallet.CurrencyCap, Loc.T(L.Shop.FormatMarks)));
        }

        SettingsRow.Draw(Loc.T(L.Shop.AtRunEnd),
            Loc.T(L.Shop.AtRunEndHelp),
            SettingsControls.ToggleWidth,
            () => SettingsControls.DrawToggle(cfg, () => cfg.ShopAtRunEnd, value => cfg.ShopAtRunEnd = value, "##shop_runend"),
            SettingsRow.ToggleHeight);

        DrawShopNowRow();
    }

    private static void DrawShopNowRow()
    {
        var controller = Plugin.Instance.Controller;
        var label = Loc.T(L.Shop.ShopNow);
        var enabled = !controller.Running && hasSomethingToBuy;
        var width = PillButton.Width(label, FontAwesomeIcon.ShoppingCart) / ImGuiHelpers.GlobalScale;
        SettingsRow.Draw(Loc.T(L.Shop.GoNow),
            Loc.T(L.Shop.GoNowHelp),
            width,
            () =>
            {
                if (PillButton.Draw("##shop_now", label, Styling.AccentAmber, PillButton.Emphasis.Tinted,
                        FontAwesomeIcon.ShoppingCart, enabled, PillHeight))
                {
                    controller.ShopNow();
                }
            },
            PillHeight);
    }

    private static void DrawListGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Shop.GroupList));

        SettingsRow.Note(Loc.T(L.Shop.Wallet, heldMarks, heldCrystals), Styling.TextSecondary);
        SettingsRow.DrawBlock(Loc.T(L.Shop.AddItem), Loc.T(L.Shop.AddItemHelp), () => DrawAddRow(cfg));
        SettingsRow.DrawBlock(Loc.T(L.Shop.YourList), Loc.T(L.Shop.ListHelp), () => DrawList(cfg));
    }

    private static void DrawAddRow(Configuration cfg)
    {
        var catalog = ShopCatalog.All;
        if (catalog.Length == 0)
        {
            SettingsRow.Note(Loc.T(L.Shop.CatalogEmpty), Styling.AccentRose);
            return;
        }

        pickerSelection = Math.Clamp(pickerSelection, 0, catalog.Length - 1);
        Dropdown.Draw("##shop_pick", pickerLabels, ref pickerSelection, PickerWidth, PickerWidth, true);

        var picked = catalog[pickerSelection];
        var listed = IsListed(cfg.ShoppingList, picked.ItemId);
        ImGui.SameLine();
        var height = ImGui.GetFrameHeight() / ImGuiHelpers.GlobalScale;
        if (PillButton.Draw("##shop_add", Loc.T(L.Common.Add), Styling.AccentMint, PillButton.Emphasis.Tinted,
                FontAwesomeIcon.Plus, !listed, height))
        {
            cfg.ShoppingList.Add(new ShoppingListEntry { ItemId = picked.ItemId });
            cfg.SaveDebounced();
            refreshedAtMs = 0;
        }

        if (!listed)
        {
            return;
        }

        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
        {
            ImGui.TextUnformatted(Loc.T(L.Shop.AlreadyListed));
        }
    }

    private static bool IsListed(List<ShoppingListEntry> entries, uint itemId)
    {
        for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            if (entries[entryIndex].ItemId == itemId)
            {
                return true;
            }
        }

        return false;
    }

    private static void DrawList(Configuration cfg)
    {
        Refresh(cfg);
        var entries = cfg.ShoppingList;
        if (entries.Count == 0)
        {
            SettingsRow.Note(Loc.T(L.Shop.ListEmpty), Styling.TextMuted);
            return;
        }

        int? moveUp = null;
        int? moveDown = null;
        int? remove = null;
        var scale = ImGuiHelpers.GlobalScale;
        var buttonSize = ImGui.GetFrameHeight();
        var spacingX = 4f * scale;
        var controlWidth = StopControlWidth * scale;
        var clusterWidth = controlWidth + spacingX * 4f + buttonSize * 3f;
        var clusterX = SettingsGroup.InnerRightLocalX() - clusterWidth;

        for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            var row = listRows[entryIndex];

            ImGui.AlignTextToFramePadding();
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            {
                ImGui.TextUnformatted($"{entryIndex + 1}.");
            }

            ImGui.SameLine();
            var status = row.Status.Length == 0 ? string.Empty : string.Concat("  ", row.Status);
            var nameWidth = clusterX - spacingX * 2f - ImGui.GetCursorPosX() - ImGui.CalcTextSize(status).X;
            using (ImRaii.PushColor(ImGuiCol.Text, row.NameColor))
            {
                ImGui.TextUnformatted(TextDraw.Truncate(row.Name, MathF.Max(0f, nameWidth)));
            }

            if (status.Length > 0)
            {
                ImGui.SameLine(0f, 0f);
                using (ImRaii.PushColor(ImGuiCol.Text, row.StatusColor))
                {
                    ImGui.TextUnformatted(status);
                }
            }

            ImGui.SameLine(clusterX);
            DrawStopControl(cfg, entries[entryIndex], row, entryIndex, controlWidth);

            ImGui.SameLine(clusterX + controlWidth + spacingX * 2f);
            if (IconButton.Draw(FontAwesomeIcon.ArrowUp, $"##shop_up_{entryIndex}", buttonSize, tooltip: Loc.T(L.Common.MoveUp), enabled: entryIndex > 0))
            {
                moveUp = entryIndex;
            }

            ImGui.SameLine(0f, spacingX);
            if (IconButton.Draw(FontAwesomeIcon.ArrowDown, $"##shop_down_{entryIndex}", buttonSize, tooltip: Loc.T(L.Common.MoveDown), enabled: entryIndex < entries.Count - 1))
            {
                moveDown = entryIndex;
            }

            ImGui.SameLine(0f, spacingX);
            if (IconButton.Draw(FontAwesomeIcon.Times, $"##shop_remove_{entryIndex}", buttonSize, Styling.AccentRose, Loc.T(L.Common.Remove)))
            {
                remove = entryIndex;
            }
        }

        if (ListReorder.Apply(entries, moveUp, moveDown, remove))
        {
            cfg.SaveDebounced();
            refreshedAtMs = 0;
        }
    }

    private static void DrawStopControl(Configuration cfg, ShoppingListEntry entry, ListRow row, int entryIndex, float width)
    {
        if (row.IsCollectible)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextMuted))
            {
                ImGui.TextUnformatted(Loc.T(L.Shop.BuyOnce));
            }

            return;
        }

        var stopAt = entry.StopAtCount;
        ImGui.SetNextItemWidth(width);
        using (SettingsControls.PushFrameColors())
        {
            var format = stopAt == 0 ? Loc.T(L.Shop.KeepBuying) : Loc.T(L.Shop.StopAt);
            if (ImGui.SliderInt($"##shop_stop_{entryIndex}", ref stopAt, 0, ShoppingList.MaxStopAtCount, format))
            {
                entry.StopAtCount = Math.Clamp(stopAt, 0, ShoppingList.MaxStopAtCount);
                cfg.SaveDebounced();
                refreshedAtMs = 0;
            }
        }
    }

    private static void DrawLeftoverGroup(Configuration cfg)
    {
        using var group = SettingsGroup.Begin(Loc.T(L.Shop.GroupLeftover));

        leftoverSelection = Math.Max(0, Array.IndexOf(leftoverItemIds, cfg.LeftoverMarksItemId));
        SettingsRow.Draw(Loc.T(L.Shop.LeftoverItem),
            Loc.T(L.Shop.LeftoverItemHelp),
            LeftoverComboWidth,
            () =>
            {
                if (Dropdown.Draw("##shop_leftover", leftoverLabels, ref leftoverSelection, LeftoverComboWidth, LeftoverComboWidth, true))
                {
                    cfg.LeftoverMarksItemId = leftoverItemIds[leftoverSelection];
                    cfg.SaveDebounced();
                    refreshedAtMs = 0;
                }
            });

        SettingsRow.Draw(Loc.T(L.Shop.Reserve),
            Loc.T(L.Shop.ReserveHelp),
            SettingsControls.RowSliderWidth,
            () => SettingsControls.DrawIntSlider(cfg, "##shop_reserve",
                () => cfg.KeepWolfMarks, value => cfg.KeepWolfMarks = value,
                0, PvpWallet.CurrencyCap, Loc.T(L.Shop.FormatMarks)));
    }

    private static void Refresh(Configuration cfg)
    {
        var now = Environment.TickCount64;
        if (now - refreshedAtMs < RefreshMs && listRows.Length == cfg.ShoppingList.Count)
        {
            return;
        }

        refreshedAtMs = now;
        heldMarks = PvpWallet.Held(PvpCurrency.WolfMarks);
        heldCrystals = PvpWallet.Held(PvpCurrency.TrophyCrystals);
        hasSomethingToBuy = ShoppingList.PlanTrip(ShopTrigger.Always, noSkippedItems) is not null;

        var entries = cfg.ShoppingList;
        var rows = new ListRow[entries.Count];
        for (var entryIndex = 0; entryIndex < rows.Length; entryIndex++)
        {
            rows[entryIndex] = BuildRow(entries[entryIndex]);
        }

        listRows = rows;
    }

    private static ListRow BuildRow(ShoppingListEntry entry)
    {
        if (!ShopCatalog.TryFind(entry.ItemId, out var item))
        {
            return new ListRow(entry.ItemId, Loc.T(L.Shop.UnknownItem, entry.ItemId), Styling.AccentRose, false, string.Empty, Styling.TextMuted);
        }

        var name = LabelOf(item);
        if (item.IsCollectible)
        {
            return ShoppingList.IsOwned(item)
                ? new ListRow(item.ItemId, name, Styling.TextStrong, true, Loc.T(L.Shop.StatusOwned), Styling.AccentMint)
                : new ListRow(item.ItemId, name, Styling.TextStrong, true, string.Empty, Styling.TextMuted);
        }

        var held = PvpWallet.HeldCount(item.ItemId);
        if (entry.StopAtCount <= 0)
        {
            return new ListRow(item.ItemId, name, Styling.TextStrong, false, Loc.T(L.Shop.StatusHave, held), Styling.TextMuted);
        }

        var stocked = held >= entry.StopAtCount;
        return new ListRow(item.ItemId, name, Styling.TextStrong, false, Loc.T(L.Shop.StatusHaveOf, held, entry.StopAtCount),
            stocked ? Styling.AccentMint : Styling.TextMuted);
    }

    private static void EnsureLabels()
    {
        var catalog = ShopCatalog.All;
        if (ReferenceEquals(labelsLanguage, Loc.Current) && pickerLabels.Length == catalog.Length)
        {
            return;
        }

        labelsLanguage = Loc.Current;
        pickerLabels = new string[catalog.Length];
        var leftoverIds = new List<uint> { 0 };
        var leftoverNames = new List<string> { Loc.T(L.Shop.LeftoverNone) };
        for (var itemIndex = 0; itemIndex < catalog.Length; itemIndex++)
        {
            var item = catalog[itemIndex];
            pickerLabels[itemIndex] = LabelOf(item);
            if (item.Currency != PvpCurrency.WolfMarks || item.IsCollectible || item.IsEquipment)
            {
                continue;
            }

            leftoverIds.Add(item.ItemId);
            leftoverNames.Add(pickerLabels[itemIndex]);
        }

        leftoverItemIds = [.. leftoverIds];
        leftoverLabels = [.. leftoverNames];
        refreshedAtMs = 0;
    }

    private static string LabelOf(in ShopItem item)
        => item.Currency == PvpCurrency.WolfMarks
            ? Loc.T(L.Shop.CostMarks, item.Name, item.Cost)
            : Loc.T(L.Shop.CostCrystals, item.Name, item.Cost);
}
