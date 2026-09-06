using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Modes;
using AutoPvpSeriesGrind.Core.Tasks;
using AutoPvpSeriesGrind.Windows.Components;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

// The idle screen states the whole run as one sentence. Every underlined word is a token that
// unfurls the popover editing exactly that part of the plan, so a run is configured by reading it.
internal static class PlanCard
{
    private const float PadX = 18f;
    private const float PadY = 16f;
    private const float TokenHeight = 30f;
    private const float TokenPadX = 11f;
    private const float ChevronGap = 6f;
    private const float WordGap = 8f;
    private const float LineGap = 10f;
    private const float PopoverWidth = 420f;
    private const float PopoverSegmentHeight = 40f;
    private const float PopoverGap = 6f;
    private const float PopoverRevealMs = 220f;
    private const float PopoverSlide = 8f;

    private const string ActivityPopup = "##apsg_activity_popover";
    private const string GoalPopup = "##apsg_goal_popover";
    private const string AfterPopup = "##apsg_after_popover";

    private static readonly AfterRunAction[] afterRunOrder =
        [AfterRunAction.StayLoggedIn, AfterRunAction.ReturnToInn, AfterRunAction.Logout, AfterRunAction.CloseGame];

    private static readonly (LocString Token, LocString Name, LocString Detail)[] afterRunChoices =
    [
        (L.Plan.AfterStayPhrase,   L.Plan.AfterStayLabel,   L.Plan.AfterStayHelp),
        (L.Plan.AfterInnPhrase,    L.Plan.AfterInnLabel,    L.Plan.AfterInnHelp),
        (L.Plan.AfterLogoutPhrase, L.Plan.AfterLogoutLabel, L.Plan.AfterLogoutHelp),
        (L.Plan.AfterClosePhrase,  L.Plan.AfterCloseLabel,  L.Plan.AfterCloseHelp),
    ];

    private static readonly Segmented.Item[] modeItems = new Segmented.Item[4];
    private static readonly Piece[] pieces = new Piece[7];
    private static readonly Vector2 PopoverPadding = new(16f, 16f);

    private static Vector2 activityAnchor;
    private static Vector2 goalAnchor;
    private static Vector2 afterAnchor;
    private static long activityOpenedTick;
    private static long goalOpenedTick;
    private static long afterOpenedTick;

    private enum PieceKind { Word, Activity, Goal, After }

    private readonly record struct Piece(PieceKind Kind, string Text);

    public static void Draw(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var padX = PadX * scale;
        var padY = PadY * scale;
        var dl = ImGui.GetWindowDrawList();

        dl.ChannelsSplit(2);
        dl.ChannelsSetCurrent(1);

        var y = origin.Y + padY;
        var planLabel = Loc.T(L.Plan.Title);
        var labelSize = TextDraw.SectionTitleSize(planLabel);
        TextDraw.SectionTitle(planLabel, new Vector2(origin.X + padX, y), Styling.TextStrong);
        y += labelSize.Y + 10f * scale;

        DrawSentence(cfg, ctrl, new Vector2(origin.X + padX, y), width - padX * 2f, out var sentenceBottom);
        y = sentenceBottom + 14f * scale;

        ImGui.SetCursorScreenPos(new Vector2(origin.X + padX, y));
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 6f) * scale))
        {
            ImGui.PushID("##apsg_plan_chips");
            ImGui.BeginGroup();
            DrawChips(cfg);
            ImGui.EndGroup();
            ImGui.PopID();
        }

        var end = new Vector2(origin.X + width, ImGui.GetItemRectMax().Y + padY);

        dl.ChannelsSetCurrent(0);
        Paint.Glass(dl, origin, end, Styling.PanelRounding * scale, Styling.AccentViolet, 0.07f, 0f, elevated: true);
        dl.ChannelsMerge();

        ImGui.SetCursorScreenPos(origin);
        ImGui.Dummy(new Vector2(width, end.Y - origin.Y));

        DrawActivityPopover();
        DrawGoalPopover(cfg);
        DrawAfterPopover(cfg);
    }

    private static void DrawSentence(Configuration cfg, AutoPvpSeriesController ctrl, Vector2 start, float maxWidth, out float bottom)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var tokenHeight = TokenHeight * scale;
        var wordGap = WordGap * scale;
        var endless = cfg.ActiveMode.Id == EndlessMode.ModeId;

        var count = 0;
        pieces[count++] = new Piece(PieceKind.Word, Loc.T(L.Plan.Queue));
        pieces[count++] = new Piece(PieceKind.Activity, Loc.T(L.Plan.Mode));
        pieces[count++] = new Piece(PieceKind.Word, Loc.T(L.Plan.SentenceUntil));
        pieces[count++] = new Piece(PieceKind.Goal, GoalLabel(cfg));
        if (!endless)
        {
            pieces[count++] = new Piece(PieceKind.Word, Loc.T(L.Plan.SentenceThen));
            pieces[count++] = new Piece(PieceKind.After, Loc.T(afterRunChoices[AfterIndex(cfg)].Token));
        }

        pieces[count++] = new Piece(PieceKind.Word, Loc.T(L.Plan.SentenceEnd));

        var x = start.X;
        var y = start.Y;
        var editable = !ctrl.Running;

        for (var index = 0; index < count; index++)
        {
            var piece = pieces[index];
            var isPunctuation = piece.Kind == PieceKind.Word && piece.Text == ".";
            var pieceWidth = piece.Kind == PieceKind.Word ? TextDraw.Measure(piece.Text).X : TokenWidth(piece.Text);
            var gap = isPunctuation ? 0f : wordGap;
            if (x > start.X && x + pieceWidth > start.X + maxWidth)
            {
                x = start.X;
                y += tokenHeight + LineGap * scale;
            }

            if (piece.Kind == PieceKind.Word)
            {
                var textSize = TextDraw.Measure(piece.Text);
                TextDraw.At(piece.Text, new Vector2(x, y + (tokenHeight - textSize.Y) * 0.5f), Styling.TextSecondary);
            }
            else
            {
                ImGui.SetCursorScreenPos(new Vector2(x, y));
                var clicked = DrawToken(TokenId(piece.Kind), piece.Text, Styling.AccentViolet, editable);
                var anchor = new Vector2(x, y + tokenHeight + PopoverGap * scale);
                switch (piece.Kind)
                {
                    case PieceKind.Activity:
                        activityAnchor = anchor;
                        if (clicked) activityOpenedTick = OpenPopover(ActivityPopup);
                        break;
                    case PieceKind.Goal:
                        goalAnchor = anchor;
                        if (clicked) goalOpenedTick = OpenPopover(GoalPopup);
                        break;
                    case PieceKind.After:
                        afterAnchor = anchor;
                        if (clicked) afterOpenedTick = OpenPopover(AfterPopup);
                        break;
                }
            }

            x += pieceWidth + gap;
        }

        bottom = y + tokenHeight;
    }

    private static void DrawChips(Configuration cfg)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var rank = PvpProfileReader.SeriesCurrentRank();
        Chip.Draw(Loc.T(L.Plan.SeriesRank, rank), Styling.AccentAmber, FontAwesomeIcon.Medal);

        if (MatchState.LocalIsMelee())
        {
            ImGui.SameLine(0f, 6f * scale);
            Chip.Draw(Loc.T(L.Plan.RangedFaster), Styling.AccentAmberSoft, FontAwesomeIcon.Lightbulb,
                tooltip: Loc.T(L.Plan.RangedFasterHelp));
        }

        if (cfg.TakeBreaks)
        {
            ImGui.SameLine(0f, 6f * scale);
            Chip.Draw(Loc.T(L.Plan.BreakEvery, cfg.BreakEveryMatches), Styling.AccentMint, FontAwesomeIcon.Coffee,
                tooltip: Loc.T(L.Plan.BreakEveryHelp, cfg.BreakMinutes, cfg.BreakEveryMatches));
        }
    }

    private static string TokenId(PieceKind kind) => kind switch
    {
        PieceKind.Activity => "##apsg_token_activity",
        PieceKind.Goal     => "##apsg_token_goal",
        _                  => "##apsg_token_after",
    };

    private static float TokenWidth(string label)
    {
        var scale = ImGuiHelpers.GlobalScale;
        return TokenPadX * 2f * scale + TextDraw.Measure(label).X + ChevronGap * scale + TextDraw.IconSize(FontAwesomeIcon.ChevronDown).X;
    }

    private static bool DrawToken(string id, string label, Vector4 accent, bool enabled)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var size = new Vector2(TokenWidth(label), TokenHeight * scale);
        var origin = ImGui.GetCursorScreenPos();
        var end = origin + size;
        var hit = Hit.Area(id, size, enabled);
        var hover = Motion.Hover(Motion.Key(id), hit.Hovered);
        var dl = ImGui.GetWindowDrawList();

        var fill = enabled ? Styling.WithAlpha(accent, 0.16f + 0.12f * hover) : Styling.WithAlpha(Styling.Surface2, 0.8f);
        var border = enabled ? Styling.WithAlpha(accent, 0.45f + 0.30f * hover) : Styling.WithAlpha(Styling.BorderDim, 0.6f);
        var text = enabled ? Vector4.Lerp(Styling.Lighten(accent, 0.3f), Styling.TextStrong, hover * 0.5f) : Styling.TextDim;
        if (hit.Held) fill = Styling.Darken(fill, 0.12f);
        Paint.Pill(dl, origin, end, fill, border);

        var midY = origin.Y + size.Y * 0.5f;
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, new Vector2(origin.X + TokenPadX * scale, midY - labelSize.Y * 0.5f), text);

        var chevronSize = TextDraw.IconSize(FontAwesomeIcon.ChevronDown);
        TextDraw.Icon(FontAwesomeIcon.ChevronDown, new Vector2(end.X - TokenPadX * scale - chevronSize.X, midY - chevronSize.Y * 0.5f + 1f * scale),
            Styling.WithAlpha(text, 0.75f));

        if (!enabled && Hit.HoveringRect(origin, end)) Tooltip.Show(Loc.T(L.Plan.Locked));
        return hit.Clicked;
    }

    private static string GoalLabel(Configuration cfg) => cfg.ActiveMode.Id switch
    {
        MatchCountMode.ModeId => Loc.T(L.Plan.GoalMatches, cfg.TargetMatchCount),
        SeriesRankMode.ModeId => Loc.T(L.Plan.GoalRank, cfg.TargetSeriesRank),
        TimeBoxedMode.ModeId  => Loc.T(L.Plan.GoalMinutes, cfg.TargetMinutes),
        _                     => Loc.T(L.Plan.GoalEndless),
    };

    private static int AfterIndex(Configuration cfg) => Math.Max(0, Array.IndexOf(afterRunOrder, cfg.AfterRun));

    private static void RefreshModeItems(IReadOnlyList<ISeriesGrindMode> modes)
    {
        for (var index = 0; index < modes.Count && index < modeItems.Length; index++)
        {
            modeItems[index] = modes[index].Id switch
            {
                MatchCountMode.ModeId => new Segmented.Item(FontAwesomeIcon.ListOl, Loc.T(L.Plan.TabMatches)),
                SeriesRankMode.ModeId => new Segmented.Item(FontAwesomeIcon.Medal, Loc.T(L.Plan.TabRank)),
                TimeBoxedMode.ModeId  => new Segmented.Item(FontAwesomeIcon.Stopwatch, Loc.T(L.Plan.TabTime)),
                EndlessMode.ModeId    => new Segmented.Item(FontAwesomeIcon.Infinity, Loc.T(L.Plan.TabEndless)),
                _                     => new Segmented.Item(FontAwesomeIcon.Flag, modes[index].DisplayName),
            };
        }
    }

    private static Vector2 PopoverPosition(Vector2 anchor, float contentWidth)
    {
        var viewport = ImGui.GetMainViewport();
        var popupWidth = contentWidth + PopoverPadding.X * 2f * ImGuiHelpers.GlobalScale;
        var maxX = viewport.WorkPos.X + viewport.WorkSize.X - popupWidth;
        return anchor with { X = MathF.Max(viewport.WorkPos.X, MathF.Min(anchor.X, maxX)) };
    }

    private static long OpenPopover(string id)
    {
        ImGui.OpenPopup(id);
        return Environment.TickCount64;
    }

    private ref struct Popover
    {
        private ImRaii.StyleDisposable style;
        private ImRaii.PopupDisposable popup;

        public Popover(string id, Vector2 anchor, float width, long openedTick)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var reveal = Motion.Reveal(openedTick, PopoverRevealMs);
            var position = PopoverPosition(anchor, width) - new Vector2(0f, (1f - reveal) * PopoverSlide * scale);
            ImGui.SetNextWindowPos(position, ImGuiCond.Always);
            style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, PopoverPadding * scale)
                .Push(ImGuiStyleVar.Alpha, MathF.Max(0.05f, reveal));
            popup = ImRaii.Popup(id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove);
        }

        public bool Open => popup.Alive;

        public void Dispose()
        {
            popup.Dispose();
            style.Dispose();
        }
    }

    private static void DrawActivityPopover()
    {
        if (!ImGui.IsPopupOpen(ActivityPopup)) return;

        var scale = ImGuiHelpers.GlobalScale;
        var width = PopoverWidth * scale;
        using var popover = new Popover(ActivityPopup, activityAnchor, width, activityOpenedTick);
        if (!popover.Open) return;

        Heading(Loc.T(L.Plan.WhatToQueue), width);
        if (DrawChoiceRow(0, Loc.T(L.Plan.Mode), Loc.T(L.Plan.QueueCasualHelp), true, width))
        {
            ImGui.CloseCurrentPopup();
        }

        DrawLockedRow(1, Loc.T(L.Plan.QueueFrontline), Loc.T(L.Plan.QueueFrontlineHelp), width);
    }

    private static void DrawGoalPopover(Configuration cfg)
    {
        if (!ImGui.IsPopupOpen(GoalPopup)) return;

        var scale = ImGuiHelpers.GlobalScale;
        var modes = SeriesGrindModes.All;
        var visible = Math.Min(modes.Count, modeItems.Length);
        RefreshModeItems(modes);
        var width = MathF.Max(PopoverWidth * scale, Segmented.PreferredWidth(modeItems.AsSpan(0, visible)));

        using var popover = new Popover(GoalPopup, goalAnchor, width, goalOpenedTick);
        if (!popover.Open) return;

        var selected = 0;
        for (var index = 0; index < modes.Count; index++)
        {
            if (modes[index].Id == cfg.ActiveMode.Id) selected = index;
        }

        if (Segmented.Draw("##apsg_modes", modeItems.AsSpan(0, visible), ref selected, height: PopoverSegmentHeight, width: width))
        {
            cfg.ModeId = modes[selected].Id;
            cfg.SaveDebounced();
        }

        Styling.VSpace(14f);
        if (cfg.ActiveMode.Id == EndlessMode.ModeId)
        {
            Caption(Loc.T(L.Plan.EndlessHelp), width);
            return;
        }

        var currentRank = PvpProfileReader.SeriesCurrentRank();
        var (label, unit, step, min, max, value, note) = cfg.ActiveMode.Id switch
        {
            MatchCountMode.ModeId => (Loc.T(L.Plan.StopAfter), Loc.T(L.Plan.UnitMatches), 5, 1, 999, cfg.TargetMatchCount,
                Loc.T(L.Plan.StopAfterMatchesHelp)),
            SeriesRankMode.ModeId => (Loc.T(L.Plan.ReachRank), Loc.T(L.Plan.UnitRank), 1, Math.Clamp(currentRank + 1, 1, 30), 30, Math.Max(cfg.TargetSeriesRank, Math.Clamp(currentRank + 1, 1, 30)),
                Loc.T(L.Plan.ReachRankHelp, currentRank)),
            _                     => (Loc.T(L.Plan.StopAfter), Loc.T(L.Plan.UnitMinutes), 5, 1, 1440, cfg.TargetMinutes,
                Loc.T(L.Plan.StopAfterTimeHelp)),
        };

        var labelOrigin = ImGui.GetCursorScreenPos();
        var labelSize = TextDraw.Measure(label);
        TextDraw.At(label, labelOrigin, Styling.TextSecondary);
        ImGui.Dummy(new Vector2(width, labelSize.Y + 8f * scale));

        if (Stepper.Draw("##apsg_target", ref value, step, min, max, "%d"))
        {
            ApplyTarget(cfg, value);
        }

        ImGui.SameLine(0f, 10f * scale);
        ImGui.AlignTextToFramePadding();
        using (ImRaii.PushColor(ImGuiCol.Text, Styling.TextDim))
            ImGui.TextUnformatted(unit);

        Styling.VSpace(8f);
        Caption(note, width);
    }

    private static void ApplyTarget(Configuration cfg, int value)
    {
        switch (cfg.ActiveMode.Id)
        {
            case MatchCountMode.ModeId: cfg.TargetMatchCount = Math.Clamp(value, 1, 999); break;
            case SeriesRankMode.ModeId: cfg.TargetSeriesRank = Math.Clamp(value, 1, 30); break;
            default:                    cfg.TargetMinutes = Math.Clamp(value, 1, 1440); break;
        }

        cfg.SaveDebounced();
    }

    private static void DrawAfterPopover(Configuration cfg)
    {
        if (!ImGui.IsPopupOpen(AfterPopup)) return;

        var scale = ImGuiHelpers.GlobalScale;
        var width = PopoverWidth * scale;
        using var popover = new Popover(AfterPopup, afterAnchor, width, afterOpenedTick);
        if (!popover.Open) return;

        var current = AfterIndex(cfg);
        Heading(Loc.T(L.Plan.AfterGoalTitle), width);

        for (var index = 0; index < afterRunChoices.Length; index++)
        {
            if (DrawChoiceRow(index, Loc.T(afterRunChoices[index].Name), Loc.T(afterRunChoices[index].Detail), index == current, width))
            {
                cfg.AfterRun = afterRunOrder[index];
                cfg.SaveDebounced();
                ImGui.CloseCurrentPopup();
            }
        }
    }

    private static void Heading(string text, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var origin = ImGui.GetCursorScreenPos();
        var size = TextDraw.SectionTitleSize(text);
        TextDraw.SectionTitle(text, origin, Styling.TextStrong);
        ImGui.Dummy(new Vector2(width, size.Y + 8f * scale));
    }

    private static void Caption(string text, float width)
    {
        using (Fonts.PushCaption())
        {
            var origin = ImGui.GetCursorScreenPos();
            TextDraw.Wrapped(text, origin, width, Styling.TextMuted);
            ImGui.Dummy(new Vector2(width, TextDraw.MeasureWrapped(text, width).Y));
        }
    }

    private static bool DrawChoiceRow(int index, string name, string detail, bool selected, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var padX = 10f * scale;
        var padY = 8f * scale;
        var lineHeight = ImGui.GetTextLineHeight();
        float detailHeight;
        using (Fonts.PushCaption())
            detailHeight = TextDraw.Measure(detail).Y;
        var size = new Vector2(width, padY * 2f + lineHeight + 2f * scale + detailHeight);
        var origin = ImGui.GetCursorScreenPos();

        ImGui.PushID((nint)(index + 1));
        var hit = Hit.Area("##choice", size);
        var hover = Motion.Hover(Motion.Key("##choice"), hit.Hovered);
        ImGui.PopID();

        var dl = ImGui.GetWindowDrawList();
        var fill = selected ? Styling.WithAlpha(Styling.AccentViolet, 0.18f + 0.08f * hover) : Styling.WithAlpha(Styling.Surface2, 0.8f * hover);
        if (fill.W > 0.01f) Paint.Fill(dl, origin, origin + size, fill, 8f * scale);

        var nameColor = selected ? Styling.AccentVioletSoft : Vector4.Lerp(Styling.TextSecondary, Styling.TextStrong, hover);
        TextDraw.At(name, new Vector2(origin.X + padX, origin.Y + padY), nameColor);
        using (Fonts.PushCaption())
            TextDraw.At(detail, new Vector2(origin.X + padX, origin.Y + padY + lineHeight + 2f * scale), Styling.TextMuted);

        if (selected)
        {
            var checkSize = TextDraw.IconSize(FontAwesomeIcon.Check);
            TextDraw.Icon(FontAwesomeIcon.Check, new Vector2(origin.X + size.X - padX - checkSize.X, origin.Y + padY + (lineHeight - checkSize.Y) * 0.5f), Styling.AccentVioletSoft);
        }

        return hit.Clicked;
    }

    private static void DrawLockedRow(int index, string name, string detail, float width)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var padX = 10f * scale;
        var padY = 8f * scale;
        var lineHeight = ImGui.GetTextLineHeight();
        float detailHeight;
        using (Fonts.PushCaption())
            detailHeight = TextDraw.Measure(detail).Y;
        var size = new Vector2(width, padY * 2f + lineHeight + 2f * scale + detailHeight);
        var origin = ImGui.GetCursorScreenPos();

        ImGui.PushID((nint)(index + 1));
        Hit.Area("##locked", size, enabled: false);
        ImGui.PopID();

        TextDraw.At(name, new Vector2(origin.X + padX, origin.Y + padY), Styling.TextDim);
        using (Fonts.PushCaption())
            TextDraw.At(detail, new Vector2(origin.X + padX, origin.Y + padY + lineHeight + 2f * scale), Styling.TextMuted);

        var lockSize = TextDraw.IconSize(FontAwesomeIcon.Lock);
        TextDraw.Icon(FontAwesomeIcon.Lock, new Vector2(origin.X + size.X - padX - lockSize.X, origin.Y + padY + (lineHeight - lockSize.Y) * 0.5f), Styling.TextMuted);
    }
}
