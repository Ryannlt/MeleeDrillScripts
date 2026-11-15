using UnityEngine;

namespace MDS.Shared.Game
{
    // A seam, so the tests can use flat ground.
    public interface IGround
    {
        float HeightAt(Vector2 position);

        // The terrain alone, ignoring walls and platforms on it.
        float TerrainHeightAt(Vector2 position);

        // The first surface below a point at that height, so a spot beside an indoor spawn finds its floor, not the roof.
        float HeightBelow(Vector2 position, float fromY);
    }
}
