using UnityEngine;

namespace MDS.Systems
{
    // Turns steering into a BotIntent, localising a world velocity into the bot's own frame.
    public static class MovementBehaviors
    {
        private const float EpsilonSqr = 0.0001f;

        // Wander tuning. Offset > radius keeps the wander target always ahead (no spin-in-place).
        public const float DefaultWanderOffset = 2f;    // how far ahead the wander circle projects (m)
        public const float DefaultWanderRadius = 1.2f;  // wander circle radius (m); larger = sharper turns
        public const float DefaultWanderRate = 90f;     // jitter applied to the wander angle (deg/sec)
        public const float DefaultWanderDecay = 1.5f;   // pull of the wander angle back toward straight-ahead (1/sec)
        private const float MaxWanderAngle = 60f;       // hard clamp on the wander angle (deg)

        // Below this net throttle, treat the desired velocity as a stop rather than a crawl.
        private const float RestThreshold = 0.15f;

        // Localises a desired world velocity into the axis pair the engine takes.
        public static BotIntent Assemble(BotPose pose, Vector2 worldVelocity)
        {
            float mag = worldVelocity.magnitude;
            if (mag < RestThreshold) return Stop();

            Vector2 dir = worldVelocity / mag;
            float throttle = Mathf.Min(mag, 1f);
            Vector2 axis = MovementSolver.ToLocalAxis(pose, dir, throttle);
            return new BotIntent { MoveAxis = axis, LookHeading = MovementSolver.HeadingOf(dir) };
        }

        // Wander: smooth undirected roaming, Millington's steering-behaviour formulation.
        public static Vector2 WanderVelocity(BotPose pose, ref float wanderAngle, float deltaTime) =>
            WanderVelocity(pose, ref wanderAngle, DefaultWanderOffset, DefaultWanderRadius, DefaultWanderRate, DefaultWanderDecay, deltaTime);

        public static Vector2 WanderVelocity(BotPose pose, ref float wanderAngle, float offset, float radius, float rate, float decay, float deltaTime)
        {
            // Random-walk the wander angle but pull it back toward straight ahead.
            wanderAngle += RandomBinomial() * rate * deltaTime;
            wanderAngle -= wanderAngle * decay * deltaTime;
            wanderAngle = Mathf.Clamp(wanderAngle, -MaxWanderAngle, MaxWanderAngle);

            Vector2 forward = MovementSolver.DirectionFromHeading(pose.Heading);
            Vector2 circleCenter = pose.Position + forward * offset;
            Vector2 rim = MovementSolver.DirectionFromHeading(pose.Heading + wanderAngle);
            Vector2 target = circleCenter + rim * radius;

            return Steering.Seek(pose, target); // velocity toward the wandering target (offset > radius keeps it always ahead)
        }

        // Triangular random in [-1, 1], biased toward 0 (Millington's randomBinomial).
        private static float RandomBinomial() => Random.value - Random.value;

        // Rotate in place to face a world point (no translation).
        public static BotIntent FacePoint(BotPose pose, Vector2 target)
        {
            if ((target - pose.Position).sqrMagnitude < EpsilonSqr)
                return Stop(); // on top of the point: nothing meaningful to face, just halt
            return new BotIntent { MoveAxis = Vector2.zero, LookHeading = MovementSolver.HeadingTo(pose.Position, target) };
        }

        // Rotate in place to an absolute heading (degrees from North), no translation.
        public static BotIntent Face(float heading)
        {
            return new BotIntent { MoveAxis = Vector2.zero, LookHeading = heading };
        }

        // Halt translation (explicit zero axis; leaves facing as-is).
        public static BotIntent Stop()
        {
            return new BotIntent { MoveAxis = Vector2.zero };
        }
    }
}
