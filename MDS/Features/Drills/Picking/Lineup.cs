using System.Collections.Generic;
using System.Linq;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    public sealed class Lineup
    {
        public Lineup(IReadOnlyList<IPlayer> attackers, IReadOnlyList<IPlayer> defenders)
        {
            Attackers = attackers;
            Defenders = defenders;
        }

        public IReadOnlyList<IPlayer> Attackers { get; }

        public IReadOnlyList<IPlayer> Defenders { get; }

        public List<IPlayer> Everyone()
        {
            var everyone = new List<IPlayer>(Attackers);
            everyone.AddRange(Defenders);
            return everyone;
        }

        // Players compare by object, so a newcomer on a reused id isn't taken for the player who left.
        public bool Includes(IPlayer player) => Attackers.Contains(player) || Defenders.Contains(player);
    }
}
