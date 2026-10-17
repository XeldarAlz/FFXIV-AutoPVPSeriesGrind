namespace AutoPvpSeriesGrind.Core.Combat;

// Infantry support shares formation/retreat/mount policy with Frontline. Only join
// NPC objectives the team is already attacking; vehicle and lane strategy are separate.
internal sealed class RivalWingsBrain : FrontlineBrain
{
    protected override bool FollowTeamObjectivesOnly => true;
}
