using Holdfast.Modding;

namespace MDS.Shared.Players
{
    // Read-only, so only the registry changes what it knows.
    public interface IPlayer
    {
        int PlayerId { get; }

        string Name { get; }

        string RegimentTag { get; }

        bool IsBot { get; }

        // Spawned and not dead since.
        // A corpse keeps its PlayerObject, so this flag is the only reliable test.
        bool IsAlive { get; }

        // Null until the first spawn.
        SpawnState Spawn { get; }

        FactionCountry? Faction { get; }
    }
}
