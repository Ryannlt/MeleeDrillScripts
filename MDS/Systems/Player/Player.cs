using UnityEngine;
using Holdfast.Modding;

namespace MDS.Systems
{
    public class Player : IPlayer
    {
        public int PlayerId { get; private set; }
        public string PlayerName { get; private set; }
        public string RegimentTag { get; private set; }
        public bool IsBot => false;
        public bool IsAdmin { get; set; }
        public ulong SteamId { get; private set; }


        public int? SpawnSectionId { get; private set; }
        public FactionCountry? Faction { get; private set; }
        public PlayerClass? PlayerClass { get; private set; }
        public int? UniformId { get; private set; }
        public GameObject PlayerObject { get; private set; }
        public bool IsAlive { get; private set; }

        public Player(int playerId, ulong steamId, string playerName, string regimentTag, bool isAdmin)
        {
            PlayerId = playerId;
            SteamId = steamId;
            PlayerName = playerName;
            RegimentTag = regimentTag;
            IsAdmin = isAdmin;
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

        public void UpdateName(string playerName, string regimentTag)
        {
            PlayerName = playerName;
            RegimentTag = regimentTag;
        }

        public void MarkDead() => IsAlive = false;
        public void MarkAlive() => IsAlive = true;

        public override string ToString()
        {
            return $"Player ID: {PlayerId} | IsAdmin: {IsAdmin} | Name: {PlayerName} | Tag: {RegimentTag} | SteamID: {SteamId}";
        }
    }
}

