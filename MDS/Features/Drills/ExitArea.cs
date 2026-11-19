using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills
{
    // A spawn to scatter around, the drill's area to keep out of, and the spots already taken, which Around adds to.
    public sealed class ExitArea
    {
        public ExitArea(Vector2 spawn, Rect zone, List<Vector2> taken)
        {
            Spawn = spawn;
            Zone = zone;
            Taken = taken;
        }

        public Vector2 Spawn { get; }

        public Rect Zone { get; }

        public List<Vector2> Taken { get; }
    }
}
