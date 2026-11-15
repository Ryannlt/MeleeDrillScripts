using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    public sealed class SpawnRequest
    {
        // Resolved: never Random.
        public SpawnStrategy Strategy { get; set; }

        public Rect Area { get; set; }

        public float Spacing { get; set; }

        // How far in from the arena's edges the area starts, which only messages use.
        public float Offset { get; set; }

        // The compass direction from the attackers' side toward the defenders', which only Teams uses.
        public float Degrees { get; set; }
    }
}
