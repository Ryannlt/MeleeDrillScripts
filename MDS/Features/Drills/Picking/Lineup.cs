using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // The players a drill picked, each side in the order they are placed along its line.
    public sealed class Lineup
    {
        public Lineup(IReadOnlyList<IPlayer> attackers, IReadOnlyList<IPlayer> defenders)
        {
            Attackers = attackers;
            Defenders = defenders;
        }

        public IReadOnlyList<IPlayer> Attackers { get; }

        public IReadOnlyList<IPlayer> Defenders { get; }
    }
}
