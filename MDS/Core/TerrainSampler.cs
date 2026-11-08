using UnityEngine;

namespace MDS.Core
{
    public static class TerrainSampler
    {
        // The probe ray starts this far above the highest terrain sample and travels this far down, so the
        // search window spans 20m above to 20m below that sample.
        private const float RayHeightAboveTerrain = 20f;
        private const float RayLength = 40f;

        // Surfaces the ground probe may land on. Deliberately excludes players and props.
        private static readonly string[] GroundLayerNames = { "Terrain", "Static Environment", "WalkablePlatform" };
        private static readonly int GroundMask = LayerMask.GetMask(GroundLayerNames);

        // Finds the ground height at a world X,Z.
        public static float GetYAt(Vector2 position)
        {
            float terrainY = 0f;
            Terrain[] terrains = Terrain.activeTerrains;

            if (terrains != null && terrains.Length > 0)
            {
                float highestY = float.MinValue;

                foreach (Terrain terrain in terrains)
                {
                    if (terrain == null) continue;

                    float sampledY = terrain.SampleHeight(new Vector3(position.x, 0f, position.y)) + terrain.GetPosition().y;
                    if (sampledY > highestY)
                        highestY = sampledY;
                }

                if (highestY > float.MinValue)
                    terrainY = highestY; // guard: every entry was null, keep the 0 default
            }

            Vector3 origin = new Vector3(position.x, terrainY + RayHeightAboveTerrain, position.y);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hitInfo, RayLength, GroundMask))
                return hitInfo.point.y;

            Logger.Log($"Could not find surface height at ({position.x}, {position.y}). Falling back to terrain height {terrainY:F2}.", LogLevel.WARNING);
            return terrainY;
        }
    }
}
