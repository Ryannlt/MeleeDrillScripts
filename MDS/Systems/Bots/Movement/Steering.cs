using System.Collections.Generic;
using UnityEngine;

namespace MDS.Systems
{
    // The blendable steering layer. Each behaviour returns a desired world velocity.
    public static class Steering
    {
        private const float EpsilonSqr = 0.0001f;

        // Arrive radii: start slowing within SlowRadius, halt within ArriveRadius (metres).
        public const float DefaultSlowRadius = 3f;
        public const float DefaultArriveRadius = 1.5f;

        // Pursue/Evade lead: predict ahead proportional to distance, capped.
        public const float DefaultMaxPredictTime = 1.5f;
        public const float DefaultDistanceToPredictScale = 0.3f;

        // Separation comfort distance (metres): the spacing bots aim for. A neighbour at or beyond this
        // exerts no push; closer than it, the push ramps up (quadratically - see Separation).
        public const float DefaultSeparationRadius = 1.5f;

        // Collision avoidance: agents whose closest predicted approach is within this radius are avoided,
        // looking up to this many seconds ahead.
        public const float DefaultCollisionRadius = 1.2f;
        public const float DefaultCollisionLookahead = 2f;

        // Ignore pairs closing slower than this; near-stationary neighbours are Separation's job.
        public const float DefaultMinClosingSpeed = 0.75f;

        // Full-speed velocity straight at the target (zero once arrived).
        public static Vector2 Seek(BotPose pose, Vector2 target)
        {
            Vector2 toTarget = target - pose.Position;
            return toTarget.sqrMagnitude < EpsilonSqr ? Vector2.zero : toTarget.normalized;
        }

        // Full-speed velocity directly away from the threat.
        public static Vector2 Flee(BotPose pose, Vector2 threat)
        {
            Vector2 away = pose.Position - threat;
            return away.sqrMagnitude < EpsilonSqr ? Vector2.zero : away.normalized;
        }

        // Toward the target, throttle ramped down inside slowRadius; zero inside arriveRadius. The input axis
        // scales speed, so the throttle ramp alone gives a smooth stop with no absolute speed constants.
        public static Vector2 Arrive(BotPose pose, Vector2 target) =>
            Arrive(pose, target, DefaultSlowRadius, DefaultArriveRadius);

        public static Vector2 Arrive(BotPose pose, Vector2 target, float slowRadius, float arriveRadius)
        {
            Vector2 toTarget = target - pose.Position;
            float dist = toTarget.magnitude;
            if (dist < arriveRadius) return Vector2.zero;
            float throttle = dist >= slowRadius ? 1f : dist / slowRadius;
            return (toTarget / dist) * throttle;
        }

        // Pursue: seek where a moving target is heading, to lead and intercept. Zero targetVel is plain Seek.
        public static Vector2 Pursue(BotPose pose, Vector2 targetPos, Vector2 targetVel) =>
            Seek(pose, PredictTarget(pose.Position, targetPos, targetVel));

        // Evade: flee where the threat is heading, the mirror of Pursue.
        public static Vector2 Evade(BotPose pose, Vector2 threatPos, Vector2 threatVel) =>
            Flee(pose, PredictTarget(pose.Position, threatPos, threatVel));

        // Repulsion from crowding, as a comfort zone rather than a hard push.
        public static Vector2 Separation(BotPose pose, IReadOnlyList<Vector2> neighbours, float comfortRadius)
        {
            Vector2 push = Vector2.zero;
            for (int i = 0; i < neighbours.Count; i++)
            {
                Vector2 away = pose.Position - neighbours[i];
                float distSqr = away.sqrMagnitude;
                if (distSqr < 1e-6f || distSqr > comfortRadius * comfortRadius) continue;
                float dist = Mathf.Sqrt(distSqr);
                float intrusion = 1f - dist / comfortRadius; // 0 at the comfort boundary, 1 when overlapping
                push += (away / dist) * (intrusion * intrusion);
            }
            return push;
        }

        // Collision avoidance (Millington): avoid the future collision, not the current position.
        public static Vector2 CollisionAvoidance(BotPose pose, Vector2 selfVel,
            IReadOnlyList<(Vector2 pos, Vector2 vel)> neighbours, float radius, float maxLookahead)
        {
            float shortestTime = float.PositiveInfinity;
            Vector2 threatRelPos = Vector2.zero;
            Vector2 threatRelVel = Vector2.zero;
            bool haveThreat = false;

            for (int i = 0; i < neighbours.Count; i++)
            {
                Vector2 relPos = neighbours[i].pos - pose.Position;
                Vector2 relVel = neighbours[i].vel - selfVel;
                float relSpeedSqr = relVel.sqrMagnitude;
                if (relSpeedSqr < DefaultMinClosingSpeed * DefaultMinClosingSpeed) continue; // not closing / just noise

                float t = -Vector2.Dot(relPos, relVel) / relSpeedSqr; // time to closest approach
                if (t <= 0f || t > maxLookahead) continue;            // behind us, or too far ahead

                float minDist = (relPos + relVel * t).magnitude;      // separation at closest approach
                if (minDist > radius) continue;                        // passes safely

                if (t < shortestTime)
                {
                    shortestTime = t;
                    threatRelPos = relPos;
                    threatRelVel = relVel;
                    haveThreat = true;
                }
            }

            if (!haveThreat) return Vector2.zero;

            // Steer away from where the threat will be at closest approach.
            Vector2 avoidFrom = threatRelPos + threatRelVel * shortestTime;
            if (avoidFrom.sqrMagnitude < 1e-6f) avoidFrom = threatRelPos; // dead head-on: use current offset

            float urgency = 1f - Mathf.Clamp01(shortestTime / maxLookahead);
            urgency *= urgency;
            return (-avoidFrom).normalized * urgency;
        }

        // Weighted sum of steering velocities, clamped to unit magnitude.
        public static Vector2 Blend(params (Vector2 velocity, float weight)[] parts)
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < parts.Length; i++)
                sum += parts[i].velocity * parts[i].weight;
            return Vector2.ClampMagnitude(sum, 1f);
        }

        // Predicted position, leading the target by a time that grows with distance (capped).
        private static Vector2 PredictTarget(Vector2 selfPos, Vector2 targetPos, Vector2 targetVel)
        {
            float distance = (targetPos - selfPos).magnitude;
            float predictTime = Mathf.Min(distance * DefaultDistanceToPredictScale, DefaultMaxPredictTime);
            return targetPos + targetVel * predictTime;
        }
    }
}
