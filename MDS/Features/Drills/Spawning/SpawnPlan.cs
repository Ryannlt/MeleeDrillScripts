using System.Collections.Generic;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Players and Spots pair up by index; players the area had no room for are only counted.
    public sealed class SpawnPlan
    {
        public List<IPlayer> Players { get; } = new();

        public List<Vector2> Spots { get; } = new();

        public int Unplaced { get; private set; }

        public int Total => Players.Count + Unplaced;

        public bool PlacesEveryone => Unplaced == 0;

        public void Add(IReadOnlyList<IPlayer> players, IReadOnlyList<Vector2> spots)
        {
            int placed = Mathf.Min(players.Count, spots.Count);
            for (int i = 0; i < placed; i++)
            {
                Players.Add(players[i]);
                Spots.Add(spots[i]);
            }
            Unplaced += players.Count - placed;
        }
    }
}
