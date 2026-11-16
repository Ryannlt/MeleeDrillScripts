using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    public sealed class RepeatPick : ISelectionStrategy
    {
        private readonly LineupMemory _memory;
        private readonly PlayerRegistry _players;

        public RepeatPick(LineupMemory memory, PlayerRegistry players)
        {
            _memory = memory;
            _players = players;
        }

        public PickResult Pick(Headcount count, PickScope scope)
        {
            Lineup last = scope.InProgram ? _memory.LastIn(scope.Arena) : _memory.Last;
            if (last == null) return PickResult.Failed("Nothing to repeat yet: run the drill once first this round.");
            if (!count.IsEveryone && (count.Attackers != last.Attackers.Count || count.Defenders != last.Defenders.Count))
                return PickResult.Failed($"Repeat needs the same sides as last time ({last.Attackers.Count}v{last.Defenders.Count}).");

            IPlayer gone = FirstGone(last.Attackers) ?? FirstGone(last.Defenders);
            if (gone != null) return PickResult.Failed($"Can't repeat: {gone} has left.");

            IPlayer busy = last.Everyone().Find(player => !scope.IsFree(player));
            return busy == null ? PickResult.Ok(last) : PickResult.Failed($"Can't repeat: {busy} is in another fight.");
        }

        // Ids are reused network slots, so the same id can be someone new: only the same player object counts.
        private IPlayer FirstGone(IReadOnlyList<IPlayer> side)
        {
            foreach (IPlayer player in side)
            {
                if (!ReferenceEquals(_players.Find(player.PlayerId), player)) return player;
            }
            return null;
        }
    }
}
