using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Preview
{
    public sealed class DrillPreview
    {
        public List<Vector2> Attackers { get; } = new();

        public List<Vector2> Defenders { get; } = new();

        // Where open melee draws its spots from, and empty for drills that place lines.
        public Rect Area { get; set; }

        // A random draw, which the drill makes again when it runs.
        public bool IsSample { get; set; }

        // What the reply adds after the drill's name, such as the spawn strategy drawn.
        public string Detail { get; set; } = string.Empty;
    }
}
