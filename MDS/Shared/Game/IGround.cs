using UnityEngine;

namespace MDS.Shared.Game
{
    // A seam, so the tests can use flat ground.
    public interface IGround
    {
        float HeightAt(Vector2 position);
    }
}
