using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Modes;
using AutoPvpSeriesGrind.Core.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class ReadyState
{
    public enum Kind { SetupNeeded, Ready, Running, Finishing }

    public enum Stage { Preparing, Queueing, Portraits, Fighting, InMatch, Finishing }

    public readonly record struct Info(Kind Kind, Vector4 Accent, Vector4 AccentSoft, FontAwesomeIcon Icon, string Title, string Detail);

    private static int cachedFrame = -1;
    private static Info cached;

    private static int cachedStageFrame = -1;
    private static Stage cachedStage;

    public static Info Resolve(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        var frame = ImGui.GetFrameCount();
        if (frame == cachedFrame) return cached;

        cached = Compute(cfg, ctrl);
        cachedFrame = frame;
        return cached;
    }

    private static Info Compute(Configuration cfg, AutoPvpSeriesController ctrl)
    {
        if (ctrl.Running)
        {
            if (ctrl.Phase == AutoPhase.Finishing)
            {
                return new Info(Kind.Finishing, Styling.AccentMint, Styling.AccentMintSoft, FontAwesomeIcon.CheckCircle,
                    Loc.T(L.Grind.TitleWrappingUp), Loc.T(L.Grind.DetailWrappingUp));
            }

            var stage = ResolveStage(ctrl);
            var (accent, accentSoft, _) = StagePalette(stage);
            return new Info(Kind.Running, accent, accentSoft, FontAwesomeIcon.Bolt, Loc.T(L.Grind.TitleGrinding), StageDetail(stage));
        }

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            return new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft, FontAwesomeIcon.ExclamationTriangle,
                Loc.T(L.Grind.TitleAlmostThere), Loc.T(L.Grind.DetailAlmostThere));
        }

        return new Info(Kind.Ready, Styling.AccentMint, Styling.AccentMintSoft, FontAwesomeIcon.CheckCircle,
            Loc.T(L.Grind.TitleReady), Loc.T(L.Grind.DetailReady));
    }

    // Several panels ask for the stage in one frame, and each answer costs a handful of game state
    // reads, so the result is cached for the frame the same way the headline info is.
    public static Stage ResolveStage(AutoPvpSeriesController ctrl)
    {
        var frame = ImGui.GetFrameCount();
        if (frame == cachedStageFrame) return cachedStage;

        cachedStage = ComputeStage(ctrl);
        cachedStageFrame = frame;
        return cachedStage;
    }

    private static Stage ComputeStage(AutoPvpSeriesController ctrl)
    {
        if (ctrl.Phase == AutoPhase.Finishing) return Stage.Finishing;

        var inDuty = Svc.Condition[ConditionFlag.BoundByDuty];
        if (!inDuty)
        {
            return Svc.Condition[ConditionFlag.InDutyQueue] || DutyOps.IsQueued() ? Stage.Queueing : Stage.Preparing;
        }

        var timeLeft = DutyOps.ContentTimeLeft();
        if (timeLeft > Core.ApsgConstants.CrystallineConflict.IntroBandLowerSec
            && timeLeft < Core.ApsgConstants.CrystallineConflict.IntroBandUpperSec)
        {
            return Stage.Portraits;
        }

        return Svc.Condition[ConditionFlag.InCombat] ? Stage.Fighting : Stage.InMatch;
    }

    public static (Vector4 Accent, Vector4 AccentSoft, string Label) StagePalette(Stage stage) => stage switch
    {
        Stage.Preparing => (Styling.AccentBlue,   Styling.AccentBlueSoft,   Loc.T(L.Grind.StageStarting)),
        Stage.Queueing  => (Styling.AccentBlue,   Styling.AccentBlueSoft,   Loc.T(L.Grind.StageInQueue)),
        Stage.Portraits => (Styling.AccentAmber,  Styling.AccentAmberSoft,  Loc.T(L.Grind.StagePortraits)),
        Stage.Fighting  => (Styling.AccentArc, Styling.AccentArcSoft, Loc.T(L.Grind.StageFighting)),
        Stage.Finishing => (Styling.AccentMint,   Styling.AccentMintSoft,   Loc.T(L.Grind.StageDone)),
        _               => (Styling.AccentBlue,   Styling.AccentBlueSoft,   Loc.T(L.Grind.StageInMatch)),
    };

    private static string StageDetail(Stage stage) => stage switch
    {
        Stage.Preparing => Loc.T(L.Grind.StageDetailStarting),
        Stage.Queueing  => Loc.T(L.Grind.StageDetailInQueue),
        Stage.Portraits => Loc.T(L.Grind.StageDetailPortraits),
        Stage.Fighting  => Loc.T(L.Grind.StageDetailFighting),
        Stage.Finishing => Loc.T(L.Grind.StageDetailDone),
        _               => Loc.T(L.Grind.StageDetailInMatch),
    };

    public static string ShortLabel(Kind kind) => kind switch
    {
        Kind.Running     => Loc.T(L.Grind.StatusRunning),
        Kind.Finishing   => Loc.T(L.Grind.StatusWrappingUp),
        Kind.Ready       => Loc.T(L.Grind.StatusReady),
        Kind.SetupNeeded => Loc.T(L.Grind.StatusSetupNeeded),
        _                => Loc.T(L.Grind.StatusIdle),
    };

    public static string StopSummary(Configuration cfg) => cfg.ActiveMode.Id switch
    {
        MatchCountMode.ModeId => Loc.T(L.Grind.StopsAfterMatches, cfg.TargetMatchCount),
        SeriesRankMode.ModeId => Loc.T(L.Grind.StopsAtRank, cfg.TargetSeriesRank),
        TimeBoxedMode.ModeId  => Loc.T(L.Grind.StopsAfterMinutes, cfg.TargetMinutes),
        _                     => Loc.T(L.Grind.StopsOnCommand),
    };
}
