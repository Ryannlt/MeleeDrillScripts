using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // Next: each side takes the next players in its team's order, carrying on where the last pick stopped and wrapping
    // round, so repeated drills rotate through everyone. The picks are shuffled before placing, so nobody keeps the
    // same spot on the line.
    public sealed class NextPick : ISelectionStrategy
    {
        private readonly PlayerRegistry _players;
        private readonly Random _random;
        private int _attackerIndex;
        private int _defenderIndex;

        public NextPick(PlayerRegistry players, Random random)
        {
            _players = players;
            _random = random;
        }

        public PickResult Pick(Headcount count)
        {
            if (count.Attackers > _players.Attacking.Count) return PickRules.Shortfall("attackers", count.Attackers, _players.Attacking.Count);
            if (count.Defenders > _players.Defending.Count) return PickRules.Shortfall("defenders", count.Defenders, _players.Defending.Count);

            List<IPlayer> attackers = TakeNext(_players.Attacking, count.Attackers, ref _attackerIndex);
            List<IPlayer> defenders = TakeNext(_players.Defending, count.Defenders, ref _defenderIndex);
            return PickResult.Ok(new Lineup(PickRules.Shuffled(attackers, _random), PickRules.Shuffled(defenders, _random)));
        }

        public void Reset()
        {
            _attackerIndex = 0;
            _defenderIndex = 0;
        }

        private static List<IPlayer> TakeNext(IReadOnlyList<IPlayer> team, int count, ref int index)
        {
            var picked = new List<IPlayer>(count);
            for (int i = 0; i < count; i++)
            {
                index %= team.Count;
                picked.Add(team[index]);
                index++;
            }
            return picked;
        }
    }
}
