using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // Carrying on from the last pick makes repeated drills rotate through everyone.
    // Busy players are passed over and come up again on the next turn round, so whoever waited longest goes first.
    // The picks are shuffled so nobody keeps the same spot on the line.
    public sealed class NextPick : ISelectionStrategy
    {
        private readonly PlayerRegistry _players;
        private readonly Random _random;
        private readonly Rotation _attackers = new();
        private readonly Rotation _defenders = new();

        public NextPick(PlayerRegistry players, Random random)
        {
            _players = players;
            _random = random;
        }

        public PickResult Pick(Headcount count, PickScope scope)
        {
            int freeAttackers = scope.Free(_players.Attacking).Count;
            int freeDefenders = scope.Free(_players.Defending).Count;
            if (count.Attackers > freeAttackers) return PickRules.Shortfall("attackers", count.Attackers, freeAttackers);
            if (count.Defenders > freeDefenders) return PickRules.Shortfall("defenders", count.Defenders, freeDefenders);

            List<IPlayer> attackers = _attackers.Take(_players.Attacking, count.Attackers, scope);
            List<IPlayer> defenders = _defenders.Take(_players.Defending, count.Defenders, scope);
            return PickResult.Ok(new Lineup(PickRules.Shuffled(attackers, _random), PickRules.Shuffled(defenders, _random)));
        }

        public void Reset()
        {
            _attackers.Reset();
            _defenders.Reset();
        }

        // Where one team's turn order has got to.
        private sealed class Rotation
        {
            private int _index;

            public void Reset() => _index = 0;

            public List<IPlayer> Take(IReadOnlyList<IPlayer> team, int count, PickScope scope)
            {
                var picked = new List<IPlayer>(count);
                for (int seen = 0; seen < team.Count && picked.Count < count; seen++)
                {
                    _index %= team.Count;
                    if (scope.IsFree(team[_index])) picked.Add(team[_index]);
                    _index++;
                }
                return picked;
            }
        }
    }
}
