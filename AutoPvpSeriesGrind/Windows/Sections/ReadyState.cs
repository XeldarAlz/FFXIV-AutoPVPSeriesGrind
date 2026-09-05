using AutoPvpSeriesGrind.Core.External;
using AutoPvpSeriesGrind.Core.Game;
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
                    "Wrapping up", "Finishing the last steps of the run.");
            }

            var stage = ResolveStage(ctrl);
            var (accent, accentSoft, _) = StagePalette(stage);
            return new Info(Kind.Running, accent, accentSoft, FontAwesomeIcon.Bolt, "Grinding", StageDetail(stage));
        }

        if (!ExternalPlugins.AllRequiredInstalled())
        {
            return new Info(Kind.SetupNeeded, Styling.AccentRose, Styling.AccentRoseSoft, FontAwesomeIcon.ExclamationTriangle,
                "Almost there", "Install the required plugins and the bot is good to go.");
        }

        return new Info(Kind.Ready, Styling.AccentMint, Styling.AccentMintSoft, FontAwesomeIcon.CheckCircle,
            "Ready when you are", "Press Start and the queue takes it from there.");
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
        Stage.Preparing => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "Starting"),
        Stage.Queueing  => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "In queue"),
        Stage.Portraits => (Styling.AccentAmber,  Styling.AccentAmberSoft,  "Portraits"),
        Stage.Fighting  => (Styling.AccentViolet, Styling.AccentVioletSoft, "Fighting"),
        Stage.Finishing => (Styling.AccentMint,   Styling.AccentMintSoft,   "Done"),
        _               => (Styling.AccentBlue,   Styling.AccentBlueSoft,   "In match"),
    };

    private static string StageDetail(Stage stage) => stage switch
    {
        Stage.Preparing => "Getting ready to queue.",
        Stage.Queueing  => "Waiting for a Casual Match.",
        Stage.Portraits => "The match is about to start.",
        Stage.Fighting  => "Fighting for the crystal.",
        Stage.Finishing => "Finishing the last steps of the run.",
        _               => "Playing out the match.",
    };

    public static string ShortLabel(Kind kind) => kind switch
    {
        Kind.Running     => "Running",
        Kind.Finishing   => "Wrapping up",
        Kind.Ready       => "Ready",
        Kind.SetupNeeded => "Setup needed",
        _                => "Idle",
    };

    public static string StopSummary(Configuration cfg) => cfg.ActiveMode.Id switch
    {
        MatchCountMode.ModeId => $"stops after {cfg.TargetMatchCount} matches",
        SeriesRankMode.ModeId => $"stops at rank {cfg.TargetSeriesRank}",
        TimeBoxedMode.ModeId  => $"stops after {cfg.TargetMinutes} minutes",
        _                     => "stops when you stop it",
    };
}
