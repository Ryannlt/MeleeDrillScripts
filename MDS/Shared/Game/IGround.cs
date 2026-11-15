using UnityEngine;

namespace MDS.Shared.Game
{
    // The ground height at a map position, so a teleport lands on the surface. A seam: the game reads the terrain,
    // the tests use a flat map.
    public interface IGround
    {
        float HeightAt(Vector2 position);
    }
}
