using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    public enum TeamSide
    {
        Attacking,
        Defending
    }

    public enum GroupShape
    {
        Line,
        Scatter
    }

    // Players of one side standing together: a line across its facing, or a small scattered pocket.
    // The offset is in meters from the arena's center, on the same x and z as the arena's corners, before any turn.
    public sealed class DrillGroup
    {
        public TeamSide Side { get; set; }

        public int Count { get; set; }

        public Vector2 Offset { get; set; }

        // Null faces the other team.
        public Facing? Facing { get; set; }

        public float Spacing { get; set; }

        public GroupShape Shape { get; set; }

        // As it was written, so rc drill show prints a line the config or rc drill add takes back.
        public string Text { get; set; }
    }
}
