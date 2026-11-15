using Holdfast.Modding;
using UnityEngine;
using MDS.Shared.Game;
using MDS.Shared.Players;
using MDS.Shared.Rounds;
using MDS.Shared.Runtime;

// Host adapter: turns game callbacks into calls on the mod, with no logic of its own. Every callback has a default
// empty body on IHoldfastMod, so only the ones this mod uses are implemented.

namespace MDS
{
    public class MDSHoldfastMod : IHoldfastMod
    {
        private ModContext _context;

        private bool OnServer => _context != null && _context.IsServer;

        public void OnModLoaded(ModHandles handles, ModLoadReason reason)
        {
            _context = new ModContext(handles, new UnityRuntime(), GameSides.Detect(handles));
        }

        public void OnModUnloaded(ModUnloadReason reason)
        {
            _context?.Dispose();
            _context = null;
        }

        public void OnUpdateElapsedTime(float time) => _context?.Routines.Tick();

        public void PassConfigVariables(string[] value) => _context?.Config.Apply(value);

        public void OnClientModMessage(uint modId, string messageName, byte[] data)
        {
            if (_context != null && !_context.IsServer) _context.Replies.OnClientMessage(messageName, data);
        }

        public void OnRCCommand(int playerId, string input, string output, bool success)
        {
            if (!OnServer) return;

            _context.Admins.OnAdminProof(playerId);
            _context.Commands.Handle(playerId, input);
        }

        public void OnRCLogin(int playerId, string inputPassword, bool isLoggedIn)
        {
            if (OnServer && isLoggedIn) _context.Admins.OnAdminProof(playerId);
        }

        public void OnRoundDetails(int roundId, string serverName, string mapName, FactionCountry attackingFaction, FactionCountry defendingFaction, GameplayMode gameplayMode, GameType gameType)
        {
            if (!OnServer) return;

            _context.Round.OnRoundDetails(new RoundInfo
            {
                RoundId = roundId, ServerName = serverName, MapName = mapName, AttackingFaction = attackingFaction,
                DefendingFaction = defendingFaction, GameMode = gameplayMode, GameType = gameType,
            });
        }

        public void OnServerPlayerJoined(int playerId, PlayerPlatform platform, ulong steamId, string backendId, ServerPlayerDetails details)
        {
            if (!OnServer) return;

            _context.Players.OnPlayerJoined(playerId, steamId, details.DisplayName, details.RegimentTag);
            _context.Admins.OnPlayerJoined(playerId, details.IsServerAdmin);
        }

        // Obsolete, but the only callback that names a bot: OnServerPlayerJoined skips them.
        public void OnPlayerJoined(int playerId, ulong steamId, string name, string regimentTag, bool isBot)
        {
            if (OnServer && isBot) _context.Players.OnBotNamed(playerId, name, regimentTag);
        }

        public void OnServerPlayerDisplayNameChanged(int playerId, string previousDisplayName, string newDisplayName, string previousRegimentTag, string newRegimentTag)
        {
            if (OnServer) _context.Players.OnDisplayNameChanged(playerId, newDisplayName, newRegimentTag);
        }

        public void OnPlayerSpawned(int playerId, GameObject playerObject, PlayerSpawnDetails details)
        {
            if (!OnServer) return;

            var spawn = new SpawnState
            {
                SpawnSectionId = details.SpawnSectionId, Faction = details.Faction, PlayerClass = details.PlayerClass,
                UniformId = details.UniformId, PlayerObject = playerObject,
            };
            _context.Players.OnPlayerSpawned(playerId, spawn, details.IsBot);
        }

        public void OnStartSpectate(int playerId, int spectatedPlayerId)
        {
            if (OnServer) _context.Players.OnPlayerEnterSpectatorMode(playerId);
        }

        public void OnStartFreeflight(int playerId)
        {
            if (OnServer) _context.Players.OnPlayerEnterSpectatorMode(playerId);
        }

        public void OnPlayerDisconnected(int playerId)
        {
            if (OnServer) _context.Players.OnPlayerDisconnected(playerId);
        }

        public void OnPlayerHurt(int playerId, float oldHp, float newHp, EntityHealthChangedReason reason)
        {
            if (!OnServer) return;

            if (newHp <= 0f) _context.Players.OnPlayerDied(playerId);
            else _context.Players.OnPlayerSurvived(playerId, newHp);
        }

        public void OnPlayerKilledPlayer(int killerPlayerId, int victimPlayerId, EntityHealthChangedReason reason, string details)
        {
            if (OnServer) _context.Players.OnPlayerKilled(killerPlayerId, victimPlayerId);
        }
    }
}
