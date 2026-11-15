namespace MDS.Shared.Players
{
    // A carbon bot raises no join event, so it registers at its first spawn.
    public sealed class Bot : PlayerBase
    {
        public Bot(int playerId)
            : base(playerId)
        {
        }

        public override bool IsBot => true;

        public override bool KeptAcrossRounds => false;

        public override string ToString() => $"bot {PlayerId} '{RegimentTag} {Name}'";
    }
}
