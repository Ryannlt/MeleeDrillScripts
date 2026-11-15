using MDS.Shared.Logging;
using UnityEngine;

namespace MDS.Shared.Game
{
    // The highest terrain at a position, then a ray down onto whatever walkable surface is there (a wall walk, a
    // platform), so players land on top of map objects and not inside them.
    public sealed class TerrainGround : IGround
    {
        // The probe ray starts this far above the highest terrain sample and travels this far down, so the search
        // window spans 20 m above to 20 m below that sample.
        private const float RayHeightAboveTerrain = 20f;
        private const float RayLength = 40f;

        // Surfaces the probe may land on. Players and props are left out on purpose.
        private static readonly string[] GroundLayerNames = { "Terrain", "Static Environment", "WalkablePlatform" };

        private readonly ModLog _log;
        private readonly int _groundMask = LayerMask.GetMask(GroundLayerNames);

        public TerrainGround(ModLog log)
        {
            _log = log;
        }

        public float HeightAt(Vector2 position)
        {
            float terrainY = HighestTerrainAt(position);
            Vector3 origin = new Vector3(position.x, terrainY + RayHeightAboveTerrain, position.y);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, RayLength, _groundMask)) return hit.point.y;

            _log.Warning($"No surface at ({position.x:F2}, {position.y:F2}); using the terrain height {terrainY:F2}.");
            return terrainY;
        }

        // 0 when no terrain is loaded, so the ray still has somewhere to start.
        private static float HighestTerrainAt(Vector2 position)
        {
            float highest = float.MinValue;
            foreach (Terrain terrain in Terrain.activeTerrains ?? System.Array.Empty<Terrain>())
            {
                if (terrain == null) continue;

                float y = terrain.SampleHeight(new Vector3(position.x, 0f, position.y)) + terrain.GetPosition().y;
                if (y > highest) highest = y;
            }
            return highest > float.MinValue ? highest : 0f;
        }
    }
}
