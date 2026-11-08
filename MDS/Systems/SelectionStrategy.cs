using System;
using System.Collections.Generic;
using System.Linq;

namespace MDS.Systems
{
    public enum SelectionStrategyType
    {
        Random,  // Pick random players separately from each team
        Any,     // Pick random players from all players, ignoring teams
        Next,    // Pick the next available players sequentially from each team
        Repeat   // Not implemented in SelectionStrategy but useful as enum for command arugments
    }

    public static class SelectionStrategy
    {
        private readonly static Random rng = new Random();

        // Random selection: pick randomly from attacking and defending separately
        public static (List<IPlayer> attacking, List<IPlayer> defending) Random(List<IPlayer> attackingPlayers, List<IPlayer> defendingPlayers, int attackingCount, int defendingCount)
        {
            var attackingSelected = attackingPlayers.OrderBy(_ => rng.Next()).Take(attackingCount).ToList();
            var defendingSelected = defendingPlayers.OrderBy(_ => rng.Next()).Take(defendingCount).ToList();
            return (attackingSelected, defendingSelected);
        }

        // Random selection: pick randomly from all players, ignoring team
        public static (List<IPlayer> attacking, List<IPlayer> defending) Any(List<IPlayer> allPlayers, int attackingCount, int defendingCount)
        {
            var eligiblePlayers = allPlayers
                .Where(p => !StateTracker.IsSpectator(p.PlayerId))
                .ToList();

            eligiblePlayers = eligiblePlayers.OrderBy(_ => rng.Next()).ToList();

            var attackers = eligiblePlayers.Take(attackingCount).ToList();
            var defenders = eligiblePlayers.Skip(attackingCount).Take(defendingCount).ToList();

            return (attackers, defenders);
        }


        // Next selection: sequential selection based on indices (with looping)
        public static (List<IPlayer> attacking, List<IPlayer> defending) Next(List<IPlayer> attackingPlayers, List<IPlayer> defendingPlayers, int attackingCount, int defendingCount, ref int attackingIndex, ref int defendingIndex)
        {
            var attackingSelected = new List<IPlayer>();
            var defendingSelected = new List<IPlayer>();

            for (int i = 0; i < attackingCount; i++)
            {
                if (attackingPlayers.Count == 0) break;
                attackingSelected.Add(attackingPlayers[attackingIndex % attackingPlayers.Count]);
                attackingIndex++;
            }

            for (int i = 0; i < defendingCount; i++)
            {
                if (defendingPlayers.Count == 0) break;
                defendingSelected.Add(defendingPlayers[defendingIndex % defendingPlayers.Count]);
                defendingIndex++;
            }

            return (attackingSelected, defendingSelected);
        }
    }
}
