using UnityEngine;

namespace MDS.Systems
{
    // Obstacle avoidance via forward whiskers: cast a few rays and steer off whatever they hit.
    public static class ObstacleAvoidance
    {
        public const float DefaultLookahead = 2.5f;    // whisker length (m)
        public const float DefaultFeelerAngle = 30f;   // side whisker spread (deg)
        public const float DefaultCastHeight = 1f;      // ray origin height above the bot's feet (m)

        // Layers treated as solid obstacles to steer around.
        private static readonly string[] ObstacleLayerNames = { "Static Environment", "Damageable Collider" };
        private static readonly int ObstacleMask = LayerMask.GetMask(ObstacleLayerNames);

        public static Vector2 Steer(Vector3 botPosition, Vector2 travelDir) =>
            Steer(botPosition, travelDir, ObstacleMask, DefaultLookahead, DefaultFeelerAngle, DefaultCastHeight);

        public static Vector2 Steer(Vector3 botPosition, Vector2 travelDir, int mask, float lookahead, float feelerAngle, float castHeight)
        {
            if (travelDir.sqrMagnitude < 1e-6f) return Vector2.zero;
            Vector2 dir = travelDir.normalized;

            Vector3 origin = botPosition + Vector3.up * castHeight;
            Vector2[] whiskers = { dir, Rotate(dir, feelerAngle), Rotate(dir, -feelerAngle) };

            if (!TryNearestHit(origin, whiskers, mask, lookahead, out RaycastHit hit, out float hitDist))
                return Vector2.zero;

            // Steer away from the wall along its (planar) normal, stronger the closer the hit.
            Vector2 normal = new Vector2(hit.normal.x, hit.normal.z);
            if (normal.sqrMagnitude < 1e-6f) normal = -dir; // near-vertical/degenerate normal: back off
            float strength = 1f - Mathf.Clamp01(hitDist / lookahead);
            return normal.normalized * strength;
        }

        private static bool TryNearestHit(Vector3 origin, Vector2[] dirs, int mask, float lookahead,
            out RaycastHit nearest, out float nearestDist)
        {
            nearest = default;
            nearestDist = float.PositiveInfinity;
            bool any = false;

            for (int i = 0; i < dirs.Length; i++)
            {
                Vector3 d3 = new Vector3(dirs[i].x, 0f, dirs[i].y);
                if (Physics.Raycast(origin, d3, out RaycastHit hit, lookahead, mask) && hit.distance < nearestDist)
                {
                    nearest = hit;
                    nearestDist = hit.distance;
                    any = true;
                }
            }

            return any;
        }

        // Rotates a planar (XZ) direction by degrees (clockwise, matching our heading convention).
        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(r);
            float sin = Mathf.Sin(r);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
