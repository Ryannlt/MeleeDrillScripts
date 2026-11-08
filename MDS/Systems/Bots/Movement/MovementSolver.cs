using UnityEngine;

namespace MDS.Systems
{
    // Pure movement maths with no engine I/O, so it stays unit-testable.
    public static class MovementSolver
    {
        // Expresses a unit world direction in the bot's local frame.
        public static Vector2 ToLocalAxis(BotPose pose, Vector2 worldDir, float throttle)
        {
            throttle = Mathf.Clamp01(throttle);

            float hr = pose.Heading * Mathf.Deg2Rad;
            float sin = Mathf.Sin(hr);
            float cos = Mathf.Cos(hr);

            // forward = (sin, cos), right = (cos, -sin); project worldDir onto each (a -heading rotation).
            float forwards = worldDir.x * sin + worldDir.y * cos;
            float sideways = worldDir.x * cos - worldDir.y * sin;

            return new Vector2(sideways, forwards) * throttle;
        }

        // The heading (degrees from North, in [0, 360)) that points from 'from' toward 'to'.
        public static float HeadingTo(Vector2 from, Vector2 to) => HeadingOf(to - from);

        // The heading (degrees from North, in [0, 360)) a direction vector points in.
        public static float HeadingOf(Vector2 dir)
        {
            float deg = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg; // x first: 0 = North (+Z), 90 = East (+X)
            return deg < 0f ? deg + 360f : deg;
        }

        // The unit world direction (XZ) a heading points in. Inverse of HeadingOf.
        public static Vector2 DirectionFromHeading(float heading)
        {
            float hr = heading * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(hr), Mathf.Cos(hr));
        }
    }
}
