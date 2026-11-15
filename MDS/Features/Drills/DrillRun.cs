using System;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using UnityEngine;

namespace MDS.Features.Drills
{
    public sealed class DrillRun
    {
        public DrillRun(string drill, Arena arena)
        {
            Drill = drill;
            Arena = arena;
        }

        public string Drill { get; }

        public Arena Arena { get; }

        // Null until the players are placed: an open melee counts down first.
        public Lineup Lineup { get; private set; }

        // The smallest box around every spot the drill placed someone on.
        public Rect Footprint { get; private set; }

        public bool PlacedAnyone { get; private set; }

        // What the drill changed to fit its arena, told to whoever started it.
        public string Note { get; set; } = string.Empty;

        // Raised for what goes wrong after the start, such as a move short of room once a countdown ends.
        public event Action<string> Noticed;

        public void Notice(string text) => Noticed?.Invoke(text);

        public void Placed(Lineup lineup, IReadOnlyList<Vector2> spots)
        {
            Lineup = lineup;
            PlacedAnyone = spots.Count > 0;
            Footprint = PlacedAnyone ? BoundsOf(spots) : Rect.zero;
        }

        private static Rect BoundsOf(IReadOnlyList<Vector2> spots)
        {
            Vector2 min = spots[0];
            Vector2 max = spots[0];
            foreach (Vector2 spot in spots)
            {
                min = Vector2.Min(min, spot);
                max = Vector2.Max(max, spot);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
