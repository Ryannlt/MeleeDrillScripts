using Holdfast.Modding;

namespace MDS.Shared.Players
{
    // Where humans and bots differ, the registry asks the subclass instead of checking IsBot.
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

        // A new round: nobody has a body or a side until they spawn again.
        public void ClearSpawn()
        {
            Spawn = null;
            IsAlive = false;
        }

        public void MarkDead() => IsAlive = false;

        public void MarkAlive() => IsAlive = true;
    }
}
