using Holdfast.Modding;
using MDS.ConfigVariables;
using MDS.ConsoleCommands;
using MDS.Core;
using MDS.Systems;
using UnityEngine;

// Every callback here has a default empty body on IHoldfastMod, so a signature the game later changes still compiles
// and just stops being called. Only the ones MDS uses are implemented, so there are fewer to go stale.

namespace MDS
{
    public class MDSHoldfastMod : IHoldfastMod
    {
        private static bool isServer;

        public void OnModLoaded(ModHandles handles, ModLoadReason reason)
        {
            CommandExecutor.InitializeConsole(handles.Console);
            Feedback.Initialize(handles.Networking);

            // The side is asked for rather than announced: a server is handed server queries, a client client ones.
            QueryLocalIdentity identity = default;
            bool known = handles.ServerQueries != null
                ? handles.ServerQueries.TryGetLocalIdentity(out identity)
                : handles.ClientQueries != null && handles.ClientQueries.TryGetLocalIdentity(out identity);

            isServer = known && identity.IsServer;
            StateTracker.Initialize(isServer, handles.ServerQueries); // Here due to unity static class weirdness.

            if (!known) Logger.Log("The game did not say which side this is, so MDS stays client side.", LogLevel.ERROR);
            else Logger.Log(isServer ? "Running on Server." : "Running on Client.", LogLevel.INFO);
        }

        public void OnModUnloaded(ModUnloadReason reason)
        {
            ModRoutines.StopAll();
        }

        public static bool getIsServer()
        {
            return isServer;
        }

        public void OnUpdateElapsedTime(float time)
        {
            ModRoutines.Tick();
        }

        public void OnClientModMessage(uint modId, string messageName, byte[] data)
        {
            if (!isServer) Feedback.OnClientModMessage(messageName, data);
        }

        public void OnServerPlayerJoined(int playerId, PlayerPlatform platform, ulong steamId, string backendId, ServerPlayerDetails details)
        {
            if (isServer) StateTracker.OnPlayerJoined(playerId, steamId, details.DisplayName, details.RegimentTag, details.IsServerAdmin);
        }

        // Obsolete, but the only callback that names a bot: OnServerPlayerJoined skips them.
        public void OnPlayerJoined(int playerId, ulong steamId, string name, string regimentTag, bool isBot)
        {
            if (isServer && isBot) StateTracker.OnBotNamed(playerId, name, regimentTag);
        }

        public void OnServerPlayerDisplayNameChanged(int playerId, string previousDisplayName, string newDisplayName, string previousRegimentTag, string newRegimentTag)
        {
            if (isServer) StateTracker.OnDisplayNameChanged(playerId, newDisplayName, newRegimentTag);
        }

        public void OnPlayerSpawned(int playerId, GameObject playerObject, PlayerSpawnDetails details)
        {
            if (isServer) StateTracker.OnPlayerSpawned(playerId, details.SpawnSectionId, details.Faction, details.PlayerClass, details.UniformId, playerObject, details.IsBot);
        }

        public void OnRoundDetails(int roundId, string serverName, string mapName, FactionCountry attackingFaction, FactionCountry defendingFaction, GameplayMode gameplayMode, GameType gameType)
        {
            if (isServer) StateTracker.OnRoundDetails(roundId, serverName, mapName, attackingFaction, defendingFaction, gameplayMode, gameType);
        }

        public void PassConfigVariables(string[] value)
        {
            ConfigManager.ProcessConfigVariables(value);
        }

        public void OnRCLogin(int playerId, string inputPassword, bool isLoggedIn)
        {
            if (isServer) StateTracker.OnRCLogin(playerId, inputPassword, isLoggedIn);
        }

        public void OnRCCommand(int playerId, string input, string output, bool success)
        {
            if (isServer)
            {
                StateTracker.OnRCCommand(playerId, input, output, success);
                ConsoleCommandHandler.ProcessConsoleCommand(playerId, input, output, success);
            }
        }

        public void OnStartSpectate(int playerId, int spectatedPlayerId)
        {
            if (isServer) StateTracker.OnPlayerEnterSpectatorMode(playerId);
        }

        public void OnStartFreeflight(int playerId)
        {
            if (isServer) StateTracker.OnPlayerEnterSpectatorMode(playerId); //Spectator mode logic is the same as freeflight mode
        }

        public void OnPlayerDisconnected(int playerId)
        {
            if (isServer) StateTracker.OnPlayerDisconnected(playerId);
        }

        public void OnPlayerHurt(int playerId, float oldHp, float newHp, EntityHealthChangedReason reason)
        {
            if (!isServer) return;

            if (newHp <= 0f)
                StateTracker.OnPlayerDied(playerId);
            else
                StateTracker.OnPlayerSurvived(playerId, newHp);
        }

        public void OnPlayerKilledPlayer(int killerPlayerId, int victimPlayerId, EntityHealthChangedReason reason, string details)
        {
            if (!isServer) return;

            StateTracker.OnPlayerKilled(killerPlayerId, victimPlayerId);
        }
    }
}
