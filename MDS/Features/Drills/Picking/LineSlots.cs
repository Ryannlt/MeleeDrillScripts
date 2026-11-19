using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // A random pick orders each side along its line, so a plain shuffle often puts a player back where they stood last time.
    // The pick comes already shuffled, and only when that order sends someone back are a few more tried, keeping the one that sends the fewest back.
    // A place is kept as the distance from the line's middle, so it still compares when the line grows or shrinks.
    public sealed class LineSlots
    {
        // About a third of all orders send nobody back, so eight tries find one nearly every time.
        private const int Tries = 8;
        private const float Middle = 0.5f;
        private const float SamePlace = 1f;

        private readonly Random _random;
        private readonly Dictionary<IPlayer, float> _lastPlace = new();

        public LineSlots(Random random)
        {
            _random = random;
        }

        public Lineup Arrange(Lineup lineup) => new Lineup(Arrange(lineup.Attackers), Arrange(lineup.Defenders));

        public void Clear() => _lastPlace.Clear();

        private List<IPlayer> Arrange(IReadOnlyList<IPlayer> side)
        {
            var best = new List<IPlayer>(side);
            int fewest = Returning(best);
            for (int attempt = 1; attempt < Tries && fewest > 0; attempt++)
            {
                List<IPlayer> order = PickRules.Shuffled(side, _random);
                int returning = Returning(order);
                if (returning < fewest) (best, fewest) = (order, returning);
            }
            for (int i = 0; i < best.Count; i++) _lastPlace[best[i]] = PlaceOf(i, best.Count);
            return best;
        }

        private int Returning(List<IPlayer> order)
        {
            int returning = 0;
            for (int i = 0; i < order.Count; i++)
            {
                if (_lastPlace.TryGetValue(order[i], out float last) && Math.Abs(PlaceOf(i, order.Count) - last) < SamePlace) returning++;
            }
            return returning;
        }

        private static float PlaceOf(int index, int count) => index - (count - 1) * Middle;
    }
}
