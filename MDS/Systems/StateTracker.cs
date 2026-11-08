using System.Collections.Generic;
using System.Linq;
using Holdfast.Modding;
using UnityEngine;
using MDS.Core;
using MDS.ConsoleCommands;


namespace MDS.Systems
{
    public class StateTracker
    {
        private static bool _initialized = false;
        private static bool _isServer;
        private static int _roundId;
        private static string _serverName;
        private static string _mapName;
        private static FactionCountry _attackingFaction;
        private static FactionCountry _defendingFaction;
        private static GameplayMode _gameMode;
        private static GameType _gameType;

        private readonly static List<IPlayer> _allPlayers = new();
        private readonly static List<IPlayer> _attackingPlayers = new();
        private readonly static List<IPlayer> _defendingPlayers = new();
        private readonly static List<IPlayer> _spectatorPlayers = new();

        // Password logins seen before the player's join event, handed over when it arrives.
        private static readonly HashSet<int> _pendingAdmins = new();

        private static IServerModQueries _queries;
        private static readonly List<int> _connectedBuffer = new();

        // Public readonly accessors
        public static bool IsServer => _isServer;
        public static int RoundId => _roundId;
        public static string ServerName => _serverName;
        public static string MapName => _mapName;
        public static FactionCountry AttackingFaction => _attackingFaction;
        public static FactionCountry DefendingFaction => _defendingFaction;
        public static GameplayMode GameMode => _gameMode;
        public static GameType GameType => _gameType;

        public static IReadOnlyList<IPlayer> AllPlayers => _allPlayers;
        public static IReadOnlyList<IPlayer> AttackingPlayers => _attackingPlayers;
        public static IReadOnlyList<IPlayer> DefendingPlayers => _defendingPlayers;
        public static IReadOnlyList<IPlayer> SpectatorPlayers => _spectatorPlayers;

        public static void Initialize(bool server, IServerModQueries queries)
        {
            _queries = queries;

            if (!_initialized)
            {
                _initialized = true;
                _isServer = server;
                Logger.Log($"StateTracker running on {(server ? "Server" : "Client")}.", LogLevel.INFO);
            }
        }

        public static void OnRoundDetails(int roundId, string serverName, string mapName, FactionCountry attackingFaction, FactionCountry defendingFaction, GameplayMode gameplayMode, GameType gameType)
        {
            _roundId = roundId;
            _serverName = serverName;
            _mapName = mapName;
            _attackingFaction = attackingFaction;
            _defendingFaction = defendingFaction;
            _gameMode = gameplayMode;
            _gameType = gameType;

            NewRoundCleanup();
            ArenaManager.ApplyStagedArenas();
            LineManager.SpawnStagedLines(); // map-load auto-populate (after BotManager was reset above)

            Logger.Log($"Round {roundId} on {mapName}. Mode: {gameplayMode}, Type: {gameType}. Attacking: {FactionTokens.DisplayName(attackingFaction)}, Defending: {FactionTokens.DisplayName(defendingFaction)}.", LogLevel.INFO);
        }

        private static void NewRoundCleanup()
        {
            Logger.Log("Cleaning up for new round. Clearing bot data and resetting team lists.", LogLevel.INFO);

            _allPlayers.RemoveAll(p => p.IsBot);
            DropDisconnectedPlayers();
            _attackingPlayers.Clear();
            _defendingPlayers.Clear();
            _spectatorPlayers.Clear();

            foreach (var player in _allPlayers)
            {
                _spectatorPlayers.Add(player);
            }

            XvXCommand.Reset();
            GroupfightCommand.Reset();
            BotManager.Reset();
        }

        // The registry can only outlive a player who left if a disconnect was missed, and a round start is cheap to check.
        private static void DropDisconnectedPlayers()
        {
            if (_queries == null) return;

            _queries.GetConnectedPlayers(_connectedBuffer);
            for (int i = _allPlayers.Count - 1; i >= 0; i--)
            {
                int playerId = _allPlayers[i].PlayerId;
                if (!_connectedBuffer.Contains(playerId))
                {
                    Logger.Log($"Player {playerId} is no longer connected; dropping them.", LogLevel.INFO);
                    OnPlayerDisconnected(playerId);
                }
            }
        }

        // Humans only: bots raise no join event and are registered at their first spawn. Fires again every round.
        public static void OnPlayerJoined(int playerId, ulong steamId, string playerName, string regimentTag, bool isServerAdmin)
        {
            if (!IsServer) return;

            bool isAdmin = _pendingAdmins.Remove(playerId) | isServerAdmin;

            if (GetPlayerById(playerId) is Player known)
            {
                known.UpdateName(playerName, regimentTag);
                if (isAdmin) known.IsAdmin = true;
                return;
            }

            var newPlayer = new Player(playerId, steamId, playerName, regimentTag, isAdmin);
            _allPlayers.Add(newPlayer);
            _spectatorPlayers.Add(newPlayer);

            Logger.Log($"Player Joined: {newPlayer}", LogLevel.DEBUG);
        }

        public static void OnDisplayNameChanged(int playerId, string displayName, string regimentTag)
        {
            if (GetPlayerById(playerId) is Player player)
                player.UpdateName(displayName, regimentTag);
        }

        public static void OnPlayerDisconnected(int playerId)
        {
            // Everything keyed purely by id is dropped first, before the object lookups that may already be gone.
            _pendingAdmins.Remove(playerId);
            CombatTracker.Clear(playerId);
            CharacterTracker.Clear(playerId);

            var player = GetPlayerById(playerId);
            if (player == null) return;

            Logger.Log($"Player Disconnected: {player}", LogLevel.DEBUG);
            _allPlayers.Remove(player);
            _attackingPlayers.Remove(player);
            _defendingPlayers.Remove(player);
            _spectatorPlayers.Remove(player);

            if (player.IsBot)
            {
                BotManager.OnBotDisconnected(playerId);
            }
        }

        public static void OnPlayerSpawned(int playerId, int spawnSectionId, FactionCountry faction, PlayerClass playerClass, int uniformId, GameObject playerObject, bool isBot = false)
        {
            var player = GetPlayerById(playerId);

            // A bot's first spawn is the first the game says of it. Its name is never reported, so this one is ours.
            if (player == null && isBot)
            {
                player = new Bot(playerId, $"Bot {playerId}", string.Empty);
                _allPlayers.Add(player);
                BotManager.OnBotJoined(player);
                Logger.Log($"Player Joined: {player}", LogLevel.DEBUG);
            }

            if (player == null)
            {
                Logger.Log($"An untracked player {playerId} has spawned", LogLevel.ERROR);
                return;
            }

            player.AssignSpawnDetails(spawnSectionId, faction, playerClass, uniformId, playerObject);

            // If the player is spawning into the attacking team
            if (faction == AttackingFaction)
            {
                if (!_attackingPlayers.Contains(player))
                {
                    _defendingPlayers.Remove(player);
                    _spectatorPlayers.Remove(player);
                    _attackingPlayers.Add(player);
                }
            }
            // If the player is spawning into the defending team
            else if (faction == DefendingFaction)
            {
                if (!_defendingPlayers.Contains(player))
                {
                    _attackingPlayers.Remove(player);
                    _spectatorPlayers.Remove(player);
                    _defendingPlayers.Add(player);
                }
            }
            else
            {
                Logger.Log($"Invalid faction {FactionTokens.DisplayName(faction)} for player {player.PlayerName}.", LogLevel.WARNING);
            }

            Logger.Log($"Player Spawned: {player}", LogLevel.DEBUG);

            // Whoever this is, anyone fighting them was fighting the body that just stopped existing. A first
            // spawn is a no-op here, since nothing can be targeting a player who has not been in the world yet.
            BotManager.OnTargetRespawned(playerId);

            if (player.IsBot)
            {
                BotManager.OnBotSpawned(player);
            }
        }

        public static void OnPlayerDied(int playerId)
        {
            var player = GetPlayerById(playerId);
            if (player == null) return;

            // A death is reported by both the hurt and the kill event, in either order. Only the first one counts.
            bool wasAlive = player.IsAlive;
            player.MarkDead(); // the corpse's PlayerObject lingers, so flag death explicitly (bot AIs skip the dead)
            if (!wasAlive) return;

            Logger.Log($"Player {playerId} died.", LogLevel.DEBUG);

            if (player.IsBot)
                BotManager.OnBotDied(player);
        }

        // Proof of life: a hurt event that left this player above zero health.
        public static void OnPlayerSurvived(int playerId, float newHp)
        {
            var player = GetPlayerById(playerId);
            if (player == null || player.IsAlive) return;
            if (_spectatorPlayers.Contains(player)) return;

            player.MarkAlive();

            // Loud on purpose. Reaching here means we called someone dead who was not, and the only way to know
            // how often that happens is for it to say so.
            Logger.Log($"Player {playerId} was flagged dead but is still alive at {newHp} hp; clearing the flag.",
                LogLevel.WARNING);
        }

        public static void OnPlayerKilled(int killerPlayerId, int victimPlayerId)
        {
            OnPlayerDied(victimPlayerId);
            BotManager.OnPlayerKilled(killerPlayerId, victimPlayerId);
        }

        public static void OnPlayerEnterSpectatorMode(int playerId)
        {
            var player = GetPlayerById(playerId);
            if (player == null) return;

            _attackingPlayers.Remove(player);
            _defendingPlayers.Remove(player);

            // No longer a body on the field: a spectator keeps its PlayerObject, so mark it dead explicitly.
            player.MarkDead();

            if (!_spectatorPlayers.Contains(player))
                _spectatorPlayers.Add(player);

            Logger.Log($"Player {player.PlayerName} entered spectator mode.", LogLevel.DEBUG);
        }

        public static void OnRCLogin(int playerId, string inputPassword, bool isLoggedIn)
        {
            Logger.Log($"Player {playerId} login attempt: {(isLoggedIn ? "Granted" : "Denied")}", LogLevel.DEBUG);

            // Only ever grant here, never revoke. This callback fires for a typed password, and a whitelisted
            // admin who mistypes one is still an admin.
            if (isLoggedIn) GrantAdmin(playerId);
        }

        // The game only runs rc commands for logged-in admins, so a successful one is proof. It is also how a
        // password login from an earlier round, which this load of the mod never saw, gets recognized.
        public static void OnRCCommand(int playerId, string input, string output, bool success)
        {
            if (IsServer && success) GrantAdmin(playerId);
        }

        private static void GrantAdmin(int playerId)
        {
            if (GetPlayerById(playerId) is Player player)
            {
                if (player.IsAdmin) return;
                player.IsAdmin = true;
            }
            else if (!_pendingAdmins.Add(playerId))
            {
                return;
            }

            Logger.Log($"Player {playerId} marked as admin.", LogLevel.DEBUG);
            Feedback.LogOnClient(playerId, "Logged in as admin.");
        }

        // A live read of a spawned player, for command-time and debug use. False when they have no body right now.
        public static bool TryGetPlayerStatus(int playerId, out QueryServerPlayerStatus status)
        {
            status = default;
            return _queries != null && _queries.TryGetPlayerStatus(playerId, out status);
        }

        public static bool TryGetPosition(int playerId, out Vector3 position)
        {
            bool found = TryGetPlayerStatus(playerId, out QueryServerPlayerStatus status);
            position = status.Position;
            return found;
        }

        public static IPlayer GetPlayerById(int playerId)
        {
            return _allPlayers.Find(p => p.PlayerId == playerId);
        }

        public static bool IsSpectator(int playerId)
        {
            return _spectatorPlayers.Any(p => p.PlayerId == playerId);
        }

        // True for anyone on the server admin list, or who has logged in or run an rc command this session.
        public static bool IsPlayerAdmin(int playerId)
        {
            return _allPlayers.FirstOrDefault(p => p.PlayerId == playerId) is Player player && player.IsAdmin;
        }

    }
}
