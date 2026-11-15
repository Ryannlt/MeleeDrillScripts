using System.Collections.Generic;
using Holdfast.Modding;
using UnityEngine;

namespace MDS.Shared.Game
{
    // Without queries (on a client, in a unit test) it finds nothing.
    public sealed class PlayerQueries
    {
        private readonly IServerModQueries _queries;

        public PlayerQueries(IServerModQueries queries)
        {
            _queries = queries;
        }

        public bool CanQuery => _queries != null;

        // False when the player has no body right now.
        public bool TryGetStatus(int playerId, out QueryServerPlayerStatus status)
        {
            status = default;
            return _queries != null && _queries.TryGetPlayerStatus(playerId, out status);
        }

        public bool TryGetPosition(int playerId, out Vector3 position)
        {
            bool found = TryGetStatus(playerId, out QueryServerPlayerStatus status);
            position = status.Position;
            return found;
        }

        // Humans only: the game leaves bots out.
        public void GetConnectedHumans(List<int> buffer)
        {
            buffer.Clear();
            _queries?.GetConnectedPlayers(buffer);
        }
    }
}
