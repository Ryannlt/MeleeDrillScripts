using Holdfast.Modding;

namespace MDS.Shared.Players
{
    // What humans and bots share. Where they differ, the registry asks the subclass instead of checking IsBot.
    public abstract class PlayerBase : IPlayer
    {
        protected PlayerBase(int playerId)
        {
            PlayerId = playerId;
        }

        public int PlayerId { get; }

        public string Name { get; private set; } = string.Empty;

        public string RegimentTag { get; private set; } = string.Empty;

        public abstract bool IsBot { get; }

        // Whether the registry still tracks this player once a new round starts.
        public abstract bool KeptAcrossRounds { get; }

        public bool IsAlive { get; private set; }

        public SpawnState Spawn { get; private set; }

        public FactionCountry? Faction => Spawn?.Faction;

        public void Rename(string name, string regimentTag)
        {
            Name = name ?? string.Empty;
            RegimentTag = regimentTag ?? string.Empty;
        }

        public void Spawned(SpawnState spawn)
        {
            Spawn = spawn;
            IsAlive = true;
        }

        public void MarkDead() => IsAlive = false;

        public void MarkAlive() => IsAlive = true;
    }
}
