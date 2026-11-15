namespace MDS.Shared.Players
{
    // A carbon bot: it raises no join event, so it registers at its first spawn, and it's dropped when a new round
    // starts.
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
