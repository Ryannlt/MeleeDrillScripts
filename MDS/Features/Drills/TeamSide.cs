namespace MDS.Features.Drills
{
    public enum TeamSide
    {
        Attacker,
        Defender
    }

    public static class Teams
    {
        public static TeamSide Other(TeamSide side) => side == TeamSide.Attacker ? TeamSide.Defender : TeamSide.Attacker;
    }
}
