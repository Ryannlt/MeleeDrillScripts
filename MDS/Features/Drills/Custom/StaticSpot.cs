using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // One player's spot, in meters from the arena's center on the same x and z as the arena's corners, before any turn.
    public sealed class StaticSpot
    {
        public StaticSpot(TeamSide side, Vector2 offset)
        {
            Side = side;
            Offset = offset;
        }

        public TeamSide Side { get; }

        public Vector2 Offset { get; }
    }
}
