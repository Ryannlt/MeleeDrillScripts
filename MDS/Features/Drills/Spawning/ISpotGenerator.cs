using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    public interface ISpotGenerator
    {
        // Fewer than count spots when the area runs out of room at that spacing.
        List<Vector2> Generate(SpotArea area, float spacing, int count);

        // False when the area is too big to work over at that spacing, such as a grid too large to build.
        bool CanCover(Rect area, float spacing);
    }
}
