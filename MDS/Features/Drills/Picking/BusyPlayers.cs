using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // The players already in one of a program's fights: picks skip them, and claim whoever they take.
    public sealed class BusyPlayers
    {
        private readonly HashSet<IPlayer> _players = new();

        public bool Contains(IPlayer player) => _players.Contains(player);

        public void Claim(IEnumerable<IPlayer> players)
        {
            foreach (IPlayer player in players) _players.Add(player);
        }

        public void Release(IEnumerable<IPlayer> players) => _players.ExceptWith(players);
    }
}
