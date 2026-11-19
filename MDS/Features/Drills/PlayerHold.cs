using System.Collections.Generic;
using MDS.Shared.Game;
using MDS.Shared.Players;

namespace MDS.Features.Drills
{
    // Holds players still through a countdown by setting their own walk, run and jump scales to 0.
    // "reset" drops a player's own scale, so the server's scale applies again.
    public sealed class PlayerHold
    {
        private const string Still = "0";
        private const string Reset = "reset";

        private static readonly string[] Scales = { "characterWalkSpeed", "characterRunSpeed", "characterJumpForce" };

        private readonly GameConsole _console;
        private readonly List<int> _held = new();

        public PlayerHold(GameConsole console)
        {
            _console = console;
        }

        public void Hold(IEnumerable<IPlayer> players)
        {
            foreach (IPlayer player in players)
            {
                _held.Add(player.PlayerId);
                SetScales(player.PlayerId, Still);
            }
        }

        public void ReleaseAll()
        {
            foreach (int playerId in _held) SetScales(playerId, Reset);
            _held.Clear();
        }

        private void SetScales(int playerId, string value)
        {
            foreach (string scale in Scales) _console.ExecuteForCurrentRound($"set {scale} {playerId} {value}");
        }
    }
}
