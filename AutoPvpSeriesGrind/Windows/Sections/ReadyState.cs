using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Localization;
using AutoPvpSeriesGrind.Core.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using ECommons.DalamudServices;
using System.Numerics;

namespace AutoPvpSeriesGrind.Windows.Sections;

internal static class ReadyState
{
    public enum Kind { SetupNeeded, Waiting, Ready, Running, Finishing }

    public enum Stage { Preparing, Queueing, Portraits, Fighting, InMatch, Shopping, Finishing }

    public readonly record struct Info(Kind Kind, Vector4 Accent, Vector4 AccentSoft, FontAwesomeIcon Icon, string Title, string Detail);

    private static int cachedFrame = -1;
    private static Info cached;

    private static int cachedStageFrame = -1;
    private static Stage cachedStage;

    private static int cachedBlockerFrame = -1;
    private static StartBlocker cachedBlocker;

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
        if (PluginConflicts.Read() is { Blocked: true } conflict)
        {
            return new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft, FontAwesomeIcon.Ban,
                Loc.T(conflict.PluginLoaded ? L.Conflicts.Title : L.Conflicts.CheckFailedTitle),
                PluginConflicts.Reason(conflict));
        }

        if (ctrl.Running)
        {
            if (ctrl.Phase == AutoPhase.Finishing)
            {
                return new Info(Kind.Finishing, Styling.AccentMint, Styling.AccentMintSoft, FontAwesomeIcon.CheckCircle,
                    Loc.T(L.Grind.TitleWrappingUp), Loc.T(L.Grind.DetailWrappingUp));
            }

            if (ctrl.Phase == AutoPhase.Shopping)
            {
                return new Info(Kind.Running, Styling.AccentAmber, Styling.AccentAmberSoft, FontAwesomeIcon.ShoppingCart,
                    Loc.T(L.Grind.TitleShopping), Loc.T(L.Grind.DetailShopping));
            }

            var stage = ResolveStage(ctrl);
            var (accent, accentSoft, _) = StagePalette(stage);
            return new Info(Kind.Running, accent, accentSoft, FontAwesomeIcon.Bolt, Loc.T(L.Grind.TitleGrinding), StageDetail(stage, cfg));
        }

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            return new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft, FontAwesomeIcon.ExclamationTriangle,
                Loc.T(L.Grind.TitleAlmostThere), Loc.T(L.Grind.DetailAlmostThere));
        }

        var blocker = ResolveStartBlocker();
        if (blocker != StartBlocker.None)
        {
            return new Info(Kind.Waiting, Styling.AccentAmber, Styling.AccentAmberSoft, FontAwesomeIcon.HourglassHalf,
                Loc.T(StartBlockerText.Title(blocker)), Loc.T(StartBlockerText.Detail(blocker)));
        }

        return new Info(Kind.Ready, Styling.AccentMint, Styling.AccentMintSoft, FontAwesomeIcon.CheckCircle,
            Loc.T(L.Grind.TitleReady), Loc.T(L.Grind.DetailReady));
    }

    public static StartBlocker ResolveStartBlocker()
    {
        var frame = ImGui.GetFrameCount();
        if (frame == cachedBlockerFrame) return cachedBlocker;

        cachedBlocker = AutoPvpSeriesController.CheckStart();
        cachedBlockerFrame = frame;
        return cachedBlocker;
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
        if (ctrl.Phase == AutoPhase.Shopping) return Stage.Shopping;

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
        Stage.Shopping  => (Styling.AccentAmber,  Styling.AccentAmberSoft,  Loc.T(L.Grind.StageShopping)),
        Stage.Finishing => (Styling.AccentMint,   Styling.AccentMintSoft,   Loc.T(L.Grind.StageDone)),
        _               => (Styling.AccentBlue,   Styling.AccentBlueSoft,   Loc.T(L.Grind.StageInMatch)),
    };

    public static LocString FinishDetail(AutoPvpSeriesController ctrl)
    {
        if (ctrl.StopAfterMatchRequested)
        {
            return L.Shell.FinishKeepGoing;
        }

        return ResolveStage(ctrl) switch
        {
            Stage.Preparing => L.Shell.FinishIdle,
            Stage.Queueing  => L.Shell.FinishQueued,
            _               => L.Shell.FinishInMatch,
        };
    }

    public static LocString FinishTitle(AutoPvpSeriesController ctrl)
        => ctrl.StopAfterMatchRequested ? L.Shell.FinishArmed : L.Common.FinishThenStop;

    public static Vector4 FinishAccent(AutoPvpSeriesController ctrl)
        => ctrl.StopAfterMatchRequested ? Styling.AccentMint : Styling.AccentAmber;

    private static string StageDetail(Stage stage, Configuration cfg) => stage switch
    {
        Stage.Preparing => Loc.T(L.Grind.StageDetailStarting),
        Stage.Queueing  => Loc.T(cfg.MatchType switch
        {
            MatchType.Frontline => L.Grind.StageDetailInQueueFrontline,
            MatchType.RivalWings => L.Plan.QueueRivalWingsWaiting,
            MatchType.CrystallineConflict when cfg.RankedCrystallineConflict => L.Setup.RankedWaiting,
            _ => L.Grind.StageDetailInQueue,
        }),
        Stage.Portraits => Loc.T(L.Grind.StageDetailPortraits),
        Stage.Fighting  => Loc.T(L.Grind.StageDetailFighting),
        Stage.Shopping  => Loc.T(L.Grind.DetailShopping),
        Stage.Finishing => Loc.T(L.Grind.StageDetailDone),
        _               => Loc.T(L.Grind.StageDetailInMatch),
    };

    public static string ShortLabel(Kind kind) => kind switch
    {
        Kind.Running     => Loc.T(L.Grind.StatusRunning),
        Kind.Finishing   => Loc.T(L.Grind.StatusWrappingUp),
        Kind.Ready       => Loc.T(L.Grind.StatusReady),
        Kind.SetupNeeded => Loc.T(L.Grind.StatusSetupNeeded),
        Kind.Waiting     => Loc.T(L.Grind.StatusWaiting),
        _                => Loc.T(L.Grind.StatusIdle),
    };
}
