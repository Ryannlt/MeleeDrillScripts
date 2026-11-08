using Holdfast.Modding;
using MDS.Client;
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

        public void OnServerPlayerDisplayNameChanged(int playerId, string previousDisplayName, string newDisplayName, string previousRegimentTag, string newRegimentTag)
        {
            if (isServer) StateTracker.OnDisplayNameChanged(playerId, newDisplayName, newRegimentTag);
        }

        public void OnPlayerSpawned(int playerId, GameObject playerObject, PlayerSpawnDetails details)
        {
            if (isServer) StateTracker.OnPlayerSpawned(playerId, details.SpawnSectionId, details.Faction, details.PlayerClass, details.UniformId, playerObject, details.IsBot);
            if (!isServer) ClientAdminDetection.TrySpawnAdminCheck();
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
                ConsoleCommandHandler.ProcessConsoleCommand(playerId, input, output, success);
                StateTracker.OnRCCommand(playerId, input, output, success);
            }
        }

        public void OnPlayerPacket(int playerId, byte? instance, Vector3? ownerPosition, double? packetTimestamp, Vector2? ownerInputAxis, float? ownerRotationY, float? ownerPitch, float? ownerYaw, PlayerActions[] actionCollection, Vector3? cameraPosition, Vector3? cameraForward, ushort? shipID, bool swimming)
        {
            PlayerPacketAwaiter.HandlePlayerPacket(
                playerId,
                ownerPosition,
                packetTimestamp,
                ownerInputAxis,
                ownerRotationY,
                ownerPitch,
                ownerYaw,
                actionCollection,
                cameraPosition,
                cameraForward,
                shipID,
                swimming
            );

            if (isServer)
            {
                MeleeProbe.OnPacket(playerId, actionCollection, ownerRotationY, ownerPitch, ownerYaw);
                MeleeDebug.OnPacket(playerId, ownerPitch);
                CombatTracker.OnPacket(playerId, actionCollection);
            }
        }

        public void OnStartSpectate(int playerId, int spectatedPlayerId)
        {
            if (isServer) StateTracker.OnPlayerEnterSpectatorMode(playerId);
            if (!isServer) ClientAdminDetection.TrySpawnAdminCheck();
        }

        public void OnStartFreeflight(int playerId)
        {
            if (isServer) StateTracker.OnPlayerEnterSpectatorMode(playerId); //Spectator mode logic is the same as freeflight mode
            if (!isServer) ClientAdminDetection.TrySpawnAdminCheck();
        }

        public void OnPlayerDisconnected(int playerId)
        {
            if (isServer) StateTracker.OnPlayerDisconnected(playerId);
        }

        public void OnPlayerHurt(int playerId, float oldHp, float newHp, EntityHealthChangedReason reason)
        {
            if (!isServer) return;

            MeleeProbe.OnHurt(playerId, oldHp, newHp);
            MeleeDebug.OnHurt(playerId, oldHp, newHp, reason);
            if (newHp <= 0f)
                StateTracker.OnPlayerDied(playerId);
            else
                StateTracker.OnPlayerSurvived(playerId, newHp);
        }

        public void OnPlayerKilledPlayer(int killerPlayerId, int victimPlayerId, EntityHealthChangedReason reason, string details)
        {
            if (!isServer) return;

            MeleeDebug.OnKill(killerPlayerId, victimPlayerId, reason);
            StateTracker.OnPlayerKilled(killerPlayerId, victimPlayerId);
        }

        public void OnServerScorableAction(ServerScorableActionDetails details)
        {
            MeleeDebug.OnScorableAction(details);
        }

        public void OnPlayerBlock(int attackingPlayerId, int defendingPlayerId)
        {
            if (!isServer) return;

            CombatTracker.OnBlock(attackingPlayerId, defendingPlayerId);
            MeleeDebug.OnBlock(attackingPlayerId, defendingPlayerId);
        }

        public void OnPlayerClash(int attackingPlayerId, int defendingPlayerId)
        {
            if (!isServer) return;

            CombatTracker.OnClash(attackingPlayerId, defendingPlayerId);
            MeleeDebug.OnClash(attackingPlayerId, defendingPlayerId);
        }
    }
}
