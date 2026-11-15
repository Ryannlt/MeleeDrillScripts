using System;
using System.Collections.Generic;
using Holdfast.Modding;
using MDS.Shared.Game;
using MDS.Shared.Logging;
using MDS.Shared.Rounds;

namespace MDS.Shared.Players
{
    // It never calls a feature, so adding or removing one never touches this class.
    public sealed class PlayerRegistry : IDisposable
    {
        private readonly RoundTracker _round;
        private readonly PlayerQueries _queries;
        private readonly ModLog _log;
        private readonly List<PlayerBase> _all = new();
        private readonly List<PlayerBase> _attacking = new();
        private readonly List<PlayerBase> _defending = new();
        private readonly List<PlayerBase> _spectating = new();
        private readonly Dictionary<int, (string Name, string RegimentTag)> _pendingBotNames = new();
        private readonly List<int> _connected = new();

        public PlayerRegistry(RoundTracker round, PlayerQueries queries, ModLog log)
        {
            _round = round;
            _queries = queries;
            _log = log;
            _round.RoundStarted += OnRoundStarted;
        }

        public event Action<IPlayer> PlayerSpawned;

        public event Action<IPlayer> PlayerDied;

        public event Action<IPlayer> PlayerLeft;

        // Every disconnect, tracked or not: an id can carry state (an admin login) from before its join event.
        public event Action<int> IdFreed;

        // Killer id, victim id.
        public event Action<int, int> PlayerKilled;

        public IReadOnlyList<IPlayer> All => _all;

        public IReadOnlyList<IPlayer> Attacking => _attacking;

        public IReadOnlyList<IPlayer> Defending => _defending;

        public IReadOnlyList<IPlayer> Spectating => _spectating;

        public void Dispose() => _round.RoundStarted -= OnRoundStarted;

        // OnServerPlayerJoined skips bots, which register at their first spawn.
        // It fires again every round.
        public void OnPlayerJoined(int playerId, ulong steamId, string name, string regimentTag)
        {
            PlayerBase player = FindBase(playerId);
            if (player == null)
            {
                player = new Player(playerId, steamId);
                _all.Add(player);
                _spectating.Add(player);
            }

            player.Rename(name, regimentTag);
            _log.Debug($"Joined: {player}");
        }

        // The obsolete OnPlayerJoined is the only callback that names a bot, and it can arrive before the first spawn.
        public void OnBotNamed(int playerId, string name, string regimentTag)
        {
            PlayerBase bot = FindBase(playerId);
            if (bot != null) bot.Rename(name, regimentTag);
            else _pendingBotNames[playerId] = (name, regimentTag);
        }

        public void OnDisplayNameChanged(int playerId, string name, string regimentTag)
        {
            FindBase(playerId)?.Rename(name, regimentTag);
        }

        public void OnPlayerDisconnected(int playerId)
        {
            _pendingBotNames.Remove(playerId);
            IdFreed?.Invoke(playerId);
            PlayerBase player = FindBase(playerId);
            if (player == null) return;

            RemoveFromTeams(player);
            _all.Remove(player);
            player.MarkDead();
            _log.Debug($"Left: {player}");
            PlayerLeft?.Invoke(player);
        }

        public void OnPlayerSpawned(int playerId, SpawnState spawn, bool isBot)
        {
            PlayerBase player = FindBase(playerId) ?? RegisterBot(playerId, isBot);
            if (player == null)
            {
                _log.Error($"An untracked player {playerId} has spawned.");
                return;
            }

            player.Spawned(spawn);
            MoveToTeam(player, spawn.Faction);
            _log.Debug($"Spawned: {player}");
            PlayerSpawned?.Invoke(player);
        }

        // A death is reported by both the hurt and the kill event, in either order.
        // Only the first one counts.
        public void OnPlayerDied(int playerId)
        {
            PlayerBase player = FindBase(playerId);
            if (player == null || !player.IsAlive) return;

            player.MarkDead();
            PlayerDied?.Invoke(player);
        }

        // Proof of life: a hurt event that left this player above zero health.
        public void OnPlayerSurvived(int playerId, float health)
        {
            PlayerBase player = FindBase(playerId);
            if (player == null || player.IsAlive || _spectating.Contains(player)) return;

            player.MarkAlive();
            _log.Warning($"Player {playerId} was flagged dead but is alive at {health} hp; clearing the flag.");
        }

        public void OnPlayerKilled(int killerId, int victimId)
        {
            OnPlayerDied(victimId);
            PlayerKilled?.Invoke(killerId, victimId);
        }

        // A spectator keeps its PlayerObject, so it is marked dead explicitly.
        public void OnPlayerEnterSpectatorMode(int playerId)
        {
            PlayerBase player = FindBase(playerId);
            if (player == null) return;

            RemoveFromTeams(player);
            player.MarkDead();
            _spectating.Add(player);
        }

        public IPlayer Find(int playerId) => FindBase(playerId);

        private PlayerBase FindBase(int playerId)
        {
            foreach (PlayerBase player in _all)
            {
                if (player.PlayerId == playerId) return player;
            }
            return null;
        }

        private PlayerBase RegisterBot(int playerId, bool isBot)
        {
            if (!isBot) return null;

            var bot = new Bot(playerId);
            if (_pendingBotNames.TryGetValue(playerId, out var pending)) bot.Rename(pending.Name, pending.RegimentTag);
            else bot.Rename($"Bot {playerId}", string.Empty);
            _pendingBotNames.Remove(playerId);
            _all.Add(bot);
            return bot;
        }

        private void MoveToTeam(PlayerBase player, FactionCountry faction)
        {
            RoundInfo round = _round.Current;
            List<PlayerBase> team = faction == round.AttackingFaction ? _attacking
                : faction == round.DefendingFaction ? _defending
                : null;
            if (team == null)
            {
                _log.Warning($"{player} spawned for {faction}, which is neither side this round.");
                return;
            }

            RemoveFromTeams(player);
            team.Add(player);
        }

        private void RemoveFromTeams(PlayerBase player)
        {
            _attacking.Remove(player);
            _defending.Remove(player);
            _spectating.Remove(player);
        }

        private void OnRoundStarted(RoundInfo round)
        {
            _all.RemoveAll(player => !player.KeptAcrossRounds);
            _pendingBotNames.Clear();
            DropDisconnected();
            _attacking.Clear();
            _defending.Clear();
            _spectating.Clear();
            _spectating.AddRange(_all);
            foreach (PlayerBase player in _all) player.ClearSpawn();
        }

        // A player who left can only linger if a disconnect was missed, and a round start is a cheap time to check.
        // The game lists humans only, which is all that's left once the round start has dropped the bots.
        private void DropDisconnected()
        {
            if (!_queries.CanQuery) return;

            _queries.GetConnectedHumans(_connected);
            for (int i = _all.Count - 1; i >= 0; i--)
            {
                if (!_connected.Contains(_all[i].PlayerId)) OnPlayerDisconnected(_all[i].PlayerId);
            }
        }
    }
}
