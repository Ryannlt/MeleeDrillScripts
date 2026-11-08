using UnityEngine;
using Holdfast.Modding;

namespace MDS.Systems
{
    public class Bot : IPlayer
    {
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public string RegimentTag { get; private set; }
        public bool IsBot => true;
        public bool IsAdmin => false;

        public int? SpawnSectionId { get; private set; }
        public FactionCountry? Faction { get; private set; }
        public PlayerClass? PlayerClass { get; private set; }
        public int? UniformId { get; private set; }
        public GameObject PlayerObject { get; private set; }
        public bool IsAlive { get; private set; }

        public Bot(int playerId, string playerName, string regimentTag)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            RegimentTag = regimentTag;
        }

        public void AssignSpawnDetails(int spawnSectionId, FactionCountry faction, PlayerClass playerClass, int uniformId, GameObject playerObject)
        {
            SpawnSectionId = spawnSectionId;
            Faction = faction;
            PlayerClass = playerClass;
            UniformId = uniformId;
            PlayerObject = playerObject;
            IsAlive = true;
        }

        public void MarkDead() => IsAlive = false;
        public void MarkAlive() => IsAlive = true;

        public override string ToString()
        {
            return $"Bot ID: {PlayerId} | Name: {PlayerName} | Tag: {RegimentTag}";
        }

    }
}
