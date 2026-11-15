using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // Repeat: the same players as this drill's last pick, as long as all of them are still here.
    public sealed class RepeatPick : ISelectionStrategy
    {
        private readonly LineupMemory _memory;
        private readonly PlayerRegistry _players;

        public RepeatPick(LineupMemory memory, PlayerRegistry players)
        {
            _memory = memory;
            _players = players;
        }

        public PickResult Pick(Headcount count)
        {
            Lineup last = _memory.Last;
            if (last == null) return PickResult.Failed("Nothing to repeat yet: run the drill once first this round.");
            if (!count.IsEveryone && (count.Attackers != last.Attackers.Count || count.Defenders != last.Defenders.Count))
                return PickResult.Failed($"Repeat needs the same sides as last time ({last.Attackers.Count}v{last.Defenders.Count}).");

            IPlayer gone = FirstGone(last.Attackers) ?? FirstGone(last.Defenders);
            return gone == null ? PickResult.Ok(last) : PickResult.Failed($"Can't repeat: {gone} has left.");
        }

        private IPlayer FirstGone(IReadOnlyList<IPlayer> side)
        {
            foreach (IPlayer player in side)
            {
                if (_players.Find(player.PlayerId) == null) return player;
            }
            return null;
        }
    }
}
