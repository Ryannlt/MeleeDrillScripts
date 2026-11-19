using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    public sealed class RandomPick : ISelectionStrategy
    {
        private readonly PlayerRegistry _players;
        private readonly Random _random;
        private readonly LineSlots _slots;

        public RandomPick(PlayerRegistry players, Random random, LineSlots slots)
        {
            _slots = slots;
            _players = players;
            _random = random;
        }

        public PickResult Pick(Headcount count, PickScope scope)
        {
            List<IPlayer> attacking = scope.Free(_players.Attacking);
            List<IPlayer> defending = scope.Free(_players.Defending);
            int attackers = count.IsEveryone ? attacking.Count : count.Attackers;
            int defenders = count.IsEveryone ? defending.Count : count.Defenders;
            if (attackers > attacking.Count) return PickRules.Shortfall("attackers", attackers, attacking.Count);
            if (defenders > defending.Count) return PickRules.Shortfall("defenders", defenders, defending.Count);

            var lineup = new Lineup(
                PickRules.Shuffled(attacking, _random).GetRange(0, attackers),
                PickRules.Shuffled(defending, _random).GetRange(0, defenders));
            return PickResult.Ok(_slots.Arrange(lineup));
        }
    }
}
