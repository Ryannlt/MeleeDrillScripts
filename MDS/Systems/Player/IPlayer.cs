using Holdfast.Modding;
using UnityEngine;

namespace MDS.Systems
{
    public interface IPlayer
    {
        int PlayerId { get; }
        string PlayerName { get; }
        string RegimentTag { get; }
        bool IsBot { get; }
        bool IsAdmin { get; }
        GameObject PlayerObject { get; }
        FactionCountry? Faction { get; }
        PlayerClass? PlayerClass { get; }
        int? UniformId { get; }
        bool IsAlive { get; } // spawned and not yet dead; a corpse (PlayerObject still present) is NOT alive

        void AssignSpawnDetails(int spawnSectionId, FactionCountry faction, PlayerClass playerClass, int uniformId, GameObject playerObject);
        void MarkDead(); // clear IsAlive on death (StateTracker.OnPlayerDied)
        void MarkAlive(); // restore IsAlive when a reported death turns out not to have been one (StateTracker.OnPlayerSurvived)
    }
}
