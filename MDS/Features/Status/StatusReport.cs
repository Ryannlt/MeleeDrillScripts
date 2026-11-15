using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Players;
using MDS.Shared.Rounds;
using UnityEngine;

namespace MDS.Features.Status
{
    public enum PlayerGroup
    {
        All,
        Attacking,
        Defending,
        Spectator
    }

    public sealed class StatusReport
    {
        private readonly RoundTracker _round;
        private readonly PlayerRegistry _players;
        private readonly AdminRegistry _admins;
        private readonly PlayerQueries _queries;
        private readonly ArenaStore _arenas;

        public StatusReport(ModContext context, ArenaStore arenas)
        {
            _round = context.Round;
            _players = context.Players;
            _admins = context.Admins;
            _queries = context.Queries;
            _arenas = arenas;
        }

        public string Summary()
        {
            return $"MDS: round {_round.Current.RoundId} on {_round.Current.MapName}. {_arenas.Describe()} "
                + $"{_players.Attacking.Count} attacking, {_players.Defending.Count} defending, {_players.Spectating.Count} spectating.";
        }

        public string Round()
        {
            RoundInfo round = _round.Current;
            return $"Round {round.RoundId} on {round.MapName}: {round.GameMode}, {round.GameType}. "
                + $"Attacking {FactionResolver.DisplayName(round.AttackingFaction)}, defending {FactionResolver.DisplayName(round.DefendingFaction)}.";
        }

        public IReadOnlyList<IPlayer> Group(PlayerGroup group)
        {
            switch (group)
            {
                case PlayerGroup.Attacking: return _players.Attacking;
                case PlayerGroup.Defending: return _players.Defending;
                case PlayerGroup.Spectator: return _players.Spectating;
                default: return _players.All;
            }
        }

        public string Describe(IPlayer player)
        {
            string admin = _admins.IsAdmin(player.PlayerId) ? ", admin" : string.Empty;
            string side = player.Faction.HasValue ? FactionResolver.DisplayName(player.Faction.Value) : "no faction yet";
            return $"{player}{admin}, {side}, {(player.IsAlive ? "alive" : "not alive")}{Position(player.PlayerId)}";
        }

        private string Position(int playerId)
        {
            if (!_queries.TryGetPosition(playerId, out Vector3 at)) return string.Empty;

            return $" at ({Numbers.Coordinate(at.x)}, {Numbers.Coordinate(at.y)}, {Numbers.Coordinate(at.z)})";
        }
    }
}
