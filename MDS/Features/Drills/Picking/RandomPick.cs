using System;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    public sealed class RandomPick : ISelectionStrategy
    {
        private readonly PlayerRegistry _players;
        private readonly Random _random;

        public RandomPick(PlayerRegistry players, Random random)
        {
            _players = players;
            _random = random;
        }

        public PickResult Pick(Headcount count)
        {
            int attackers = count.IsEveryone ? _players.Attacking.Count : count.Attackers;
            int defenders = count.IsEveryone ? _players.Defending.Count : count.Defenders;
            if (attackers > _players.Attacking.Count) return PickRules.Shortfall("attackers", attackers, _players.Attacking.Count);
            if (defenders > _players.Defending.Count) return PickRules.Shortfall("defenders", defenders, _players.Defending.Count);

            var lineup = new Lineup(
                PickRules.Shuffled(_players.Attacking, _random).GetRange(0, attackers),
                PickRules.Shuffled(_players.Defending, _random).GetRange(0, defenders));
            return PickResult.Ok(lineup);
        }
    }
}
