using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // Any: everyone on the field in one pool, whatever their team. The first picks attack, the next defend.
    public sealed class AnyPick : ISelectionStrategy
    {
        private readonly PlayerRegistry _players;
        private readonly Random _random;

        public AnyPick(PlayerRegistry players, Random random)
        {
            _players = players;
            _random = random;
        }

        public PickResult Pick(Headcount count)
        {
            var pool = new List<IPlayer>(_players.Attacking);
            pool.AddRange(_players.Defending);
            int needed = count.Attackers + count.Defenders;
            if (needed > pool.Count) return PickRules.Shortfall("players", needed, pool.Count);

            List<IPlayer> shuffled = PickRules.Shuffled(pool, _random);
            return PickResult.Ok(new Lineup(shuffled.GetRange(0, count.Attackers), shuffled.GetRange(count.Attackers, count.Defenders)));
        }
    }
}
