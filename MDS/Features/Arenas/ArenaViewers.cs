using System;
using System.Collections.Generic;
using MDS.Shared.Game;
using MDS.Shared.Players;

namespace MDS.Features.Arenas
{
    // Each viewer gets the outlines on their own client only, and again whenever an arena changes.
    public sealed class ArenaViewers : IDisposable
    {
        private readonly HashSet<int> _viewers = new();
        private readonly ArenaStore _store;
        private readonly PlayerRegistry _players;
        private readonly Replies _replies;

        public ArenaViewers(ArenaStore store, PlayerRegistry players, Replies replies)
        {
            _store = store;
            _players = players;
            _replies = replies;
            _store.ArenaChanged += OnArenaChanged;
            _players.IdFreed += OnIdFreed;
        }

        public bool IsShowing(int playerId) => _viewers.Contains(playerId);

        public void Show(int playerId)
        {
            _viewers.Add(playerId);
            SendTo(playerId);
        }

        public void Hide(int playerId)
        {
            _viewers.Remove(playerId);
            _replies.SendToClient(playerId, ArenaViewMessage.Name, ArenaViewMessage.EncodeNothing());
        }

        public void Dispose()
        {
            _store.ArenaChanged -= OnArenaChanged;
            _players.IdFreed -= OnIdFreed;
        }

        private void SendTo(int playerId) => _replies.SendToClient(playerId, ArenaViewMessage.Name, ArenaViewMessage.Encode(_store.All));

        private void OnArenaChanged(Arena arena)
        {
            foreach (int viewer in _viewers) SendTo(viewer);
        }

        // Ids are reused network slots, so the next player in this slot mustn't inherit the outlines.
        private void OnIdFreed(int playerId) => _viewers.Remove(playerId);
    }
}
