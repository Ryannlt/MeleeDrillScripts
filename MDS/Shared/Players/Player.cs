namespace MDS.Shared.Players
{
    public sealed class Player : PlayerBase
    {
        public Player(int playerId, ulong steamId)
            : base(playerId)
        {
            SteamId = steamId;
        }

        public ulong SteamId { get; }

        public override bool IsBot => false;

        public override bool KeptAcrossRounds => true;

        public override string ToString() => $"player {PlayerId} '{RegimentTag} {Name}'";
    }
}
