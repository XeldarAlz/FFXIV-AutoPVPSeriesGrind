using AutoPvpSeriesGrind.Core.Combat;
using Newtonsoft.Json;
using System.IO;
using System.Numerics;
using System.Text;

namespace AutoPvpSeriesGrind.Core.Debug;

internal static class MatchRecorder
{
    private const string LogDirectoryName = "brain-logs";
    private const int MaxLogFiles = 30;

    private static StreamWriter? writer;
    private static long startedAtMs;
    private static bool warnedWriteFailure;

    public static bool IsRecording => writer is not null;

    public static void Begin(uint territory)
    {
        End();
        try
        {
            var directory = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, LogDirectoryName);
            Directory.CreateDirectory(directory);
            PruneOldLogs(directory);

            var fileName = $"match-{DateTime.Now:yyyyMMdd-HHmmss}-t{territory}.jsonl";
            writer = new StreamWriter(Path.Combine(directory, fileName), append: false, Encoding.UTF8) { AutoFlush = true };
            startedAtMs = Environment.TickCount64;
            warnedWriteFailure = false;
            ApsgLog.Debug($"match recorder -> {fileName}");
        }
        catch (Exception ex)
        {
            ApsgLog.Warn(ex, "MatchRecorder: failed to open log file");
            writer = null;
        }
    }

    public static void Record(PvpSnapshot snapshot, in MovePlan plan)
    {
        if (writer is null)
        {
            return;
        }
        try
        {
            writer.WriteLine(JsonConvert.SerializeObject(ToLine(snapshot, plan)));
        }
        catch (Exception ex)
        {
            if (!warnedWriteFailure)
            {
                warnedWriteFailure = true;
                ApsgLog.Warn(ex, "MatchRecorder: write failed, recording stopped");
            }
            End();
        }
    }

    public static void End()
    {
        if (writer is null)
        {
            return;
        }
        try
        {
            writer.Dispose();
        }
        catch
        {
        }
        writer = null;
    }

    private static object ToLine(PvpSnapshot snapshot, in MovePlan plan) => new
    {
        t = Environment.TickCount64 - startedAtMs,
        hp = Round2(snapshot.SelfHp),
        role = snapshot.SelfRole.ToString(),
        pos = Pos(snapshot.Self),
        posture = plan.Posture.ToString(),
        kind = plan.Kind.ToString(),
        dest = Pos(plan.Destination),
        stop = Round1(plan.StopRange),
        sprint = plan.Sprint,
        pursue = plan.Pursue,
        tgt = plan.TargetId,
        reason = plan.Reason,
        focus = snapshot.FocusCount,
        obj = snapshot.Objective is { } objective ? Pos(objective) : null,
        enemies = Actors(snapshot.Enemies, snapshot.SelfId),
        allies = Actors(snapshot.Allies, snapshot.SelfId),
    };

    private static object[] Actors(IReadOnlyList<PvpActor> actors, ulong selfId)
    {
        var lines = new object[actors.Count];
        for (var actorIndex = 0; actorIndex < actors.Count; actorIndex++)
        {
            var actor = actors[actorIndex];
            lines[actorIndex] = new
            {
                id = actor.Id,
                p = Pos(actor.Position),
                hp = Round2(actor.Hp),
                role = actor.Role.ToString(),
                me = actor.TargetId == selfId,
                g = actor.HasGuard,
                c = actor.IsCasting,
                d = Round1(actor.DistanceToSelf),
            };
        }
        return lines;
    }

    private static void PruneOldLogs(string directory)
    {
        try
        {
            var files = new DirectoryInfo(directory).GetFiles("match-*.jsonl")
                .OrderByDescending(file => file.CreationTimeUtc)
                .ToList();
            for (var fileIndex = MaxLogFiles - 1; fileIndex < files.Count; fileIndex++)
            {
                files[fileIndex].Delete();
            }
        }
        catch (Exception ex)
        {
            ApsgLog.Warn(ex, "MatchRecorder: prune failed");
        }
    }

    private static float[] Pos(Vector3 v) => [Round1(v.X), Round1(v.Y), Round1(v.Z)];

    private static float Round1(float value) => MathF.Round(value, 1);

    private static float Round2(float value) => MathF.Round(value, 2);
}
