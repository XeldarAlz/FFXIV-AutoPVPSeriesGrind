namespace AutoPvpSeriesGrind.Core.Combat;

internal sealed class ManualBrain
{
    private const float EngageRangeYalms = 25f;
    private const float BurstDropPerSec = ModerateStrategyBaseline.BurstSensitivityPercent / 100f;

    private readonly BurstMeter burst = new();
    private ulong lastTargetId;

    public bool UnderBurst { get; private set; }

    public void Reset()
    {
        burst.Reset();
        lastTargetId = 0;
        UnderBurst = false;
    }

    public ulong PickTarget(PvpSnapshot snapshot, bool picksTargets, TargetingMode mode, ulong playerTargetId)
    {
        UnderBurst = burst.DropPerSecond(snapshot.SelfHp) >= BurstDropPerSec;
        if (!picksTargets)
        {
            return playerTargetId;
        }

        var enemy = mode == TargetingMode.LowestHp
            ? TargetScoring.LowestHp(snapshot, EngageRangeYalms)
            : TargetScoring.Best(snapshot, EngageRangeYalms, lastTargetId);
        lastTargetId = enemy is { DistanceToSelf: <= EngageRangeYalms } inReach
            ? inReach.Id
            : TargetScoring.TeamObjective(snapshot, snapshot.Self, EngageRangeYalms, EngageRangeYalms)?.Id ?? enemy?.Id ?? 0;
        return lastTargetId;
    }
}
