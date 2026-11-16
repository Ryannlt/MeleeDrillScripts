using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    public sealed class PlacedGroup
    {
        public PlacedGroup(TeamSide side, List<Vector2> spots)
        {
            Side = side;
            Spots = spots;
        }

        public TeamSide Side { get; }

        public List<Vector2> Spots { get; }
    }

    // Each group's spots for one run, in the drill's group order, and what fitting the arena changed.
    public sealed class CustomPlacement
    {
        public List<PlacedGroup> Groups { get; } = new();

        public string Note { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;

        // A random turn, facing or scatter, which the drill draws again when it runs.
        public bool IsSample { get; set; }

        public bool Fits => Error.Length == 0;

        // A list, not a yield iterator: an IEnumerable iterator reads Environment.CurrentManagedThreadId, which the build's code security refuses.
        public List<Vector2> AllSpots()
        {
            var spots = new List<Vector2>();
            foreach (PlacedGroup group in Groups) spots.AddRange(group.Spots);
            return spots;
        }

        // Every spot moves toward the center by the same share, so offsets and spacing shrink together and the layout keeps its shape.
        public void ScaleToward(Vector2 center, float share)
        {
            foreach (PlacedGroup group in Groups)
            {
                for (int i = 0; i < group.Spots.Count; i++) group.Spots[i] = center + (group.Spots[i] - center) * share;
            }
        }
    }
}
