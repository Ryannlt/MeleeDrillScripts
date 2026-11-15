using System;
using System.Collections.Generic;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Picking
{
    // What the strategies share: a fair shuffle and the wording when a side is short.
    public static class PickRules
    {
        // Fisher-Yates, so every order is equally likely.
        public static List<IPlayer> Shuffled(IEnumerable<IPlayer> players, Random random)
        {
            var shuffled = new List<IPlayer>(players);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            return shuffled;
        }

        public static PickResult Shortfall(string side, int needed, int available)
        {
            return PickResult.Failed($"Not enough {side}: {needed} needed, {available} on the field.");
        }
    }
}
