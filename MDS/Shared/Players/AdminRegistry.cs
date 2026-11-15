using System;
using System.Collections.Generic;
using MDS.Shared.Logging;

namespace MDS.Shared.Players
{
    // Who may run this mod's commands. Admin is a permission, not part of who a player is, so it lives here, and every
    // entry point (rc today, a UI later) asks the same question.
    public sealed class AdminRegistry : IDisposable
    {
        private readonly HashSet<int> _admins = new();
        private readonly PlayerRegistry _players;
        private readonly ModLog _log;

        public AdminRegistry(PlayerRegistry players, ModLog log)
        {
            _players = players;
            _log = log;
            _players.PlayerLeft += OnPlayerLeft;
        }

        public void Dispose() => _players.PlayerLeft -= OnPlayerLeft;

        // The server's admin list, as the join event reports it.
        public void OnPlayerJoined(int playerId, bool isServerAdmin)
        {
            if (isServerAdmin) Grant(playerId);
        }

        // A non-admin's rc command never reaches the server, so any rc command is proof, as is a login. Only ever
        // grants: an admin who mistypes a password is still an admin.
        public void OnAdminProof(int playerId) => Grant(playerId);

        public bool IsAdmin(int playerId) => _admins.Contains(playerId);

        private void Grant(int playerId)
        {
            if (_admins.Add(playerId)) _log.Debug($"Player {playerId} is an admin.");
        }

        // Player ids are reused network slots, so the next player on this id starts out as no admin.
        private void OnPlayerLeft(IPlayer player) => _admins.Remove(player.PlayerId);
    }
}
