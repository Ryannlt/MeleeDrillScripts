using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    public sealed class AnyPick : ISelectionStrategy
    {
        private readonly PlayerRegistry _players;
        private readonly Random _random;
        private readonly LineSlots _slots;

        public AnyPick(PlayerRegistry players, Random random, LineSlots slots)
        {
            _slots = slots;
            _players = players;
            _random = random;
        }

        public PickResult Pick(Headcount count, PickScope scope)
        {
            List<IPlayer> pool = scope.Free(_players.Attacking);
            pool.AddRange(scope.Free(_players.Defending));
            int needed = count.Attackers + count.Defenders;
            if (needed > pool.Count) return PickRules.Shortfall("players", needed, pool.Count);

            List<IPlayer> shuffled = PickRules.Shuffled(pool, _random);
            return PickResult.Ok(_slots.Arrange(new Lineup(shuffled.GetRange(0, count.Attackers), shuffled.GetRange(count.Attackers, count.Defenders))));
        }
    }
}
