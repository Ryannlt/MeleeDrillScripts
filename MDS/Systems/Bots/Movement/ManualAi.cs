using System.Collections.Generic;
using UnityEngine;

namespace MDS.Systems
{
    // A manually driven AI, the test harness for the movement behaviours. Issues no orders on its own.
    public class ManualAi : IBotAi
    {
        public BotAiEnum AiType => BotAiEnum.Manual;

        // How strongly each tick's raw (position-delta) velocity pulls the smoothed target-velocity estimate.
        private const float TargetVelSmoothing = 0.25f;

        // Corrective behavior blend weights, relative to the primary steering (weight 1).
        private const float SeparationWeight = 1.5f;
        private const float AvoidWeight = 2f;      // obstacle avoidance, strong so it wins near a wall
        private const float DodgeWeight = 1.2f;    // collision avoidance, also urgency-scaled inside the behavior

        private MoveOrder _order = MoveOrder.Stop();
        private float _wanderAngle;   // persistent state for Wander; reset on each new order

        // Run is a sticky engine mode set once, not per tick.
        private bool _runStatePending;

        // Target-velocity estimate for Pursue/Evade (world XZ units/sec), tracked across ticks.
        private Vector2 _lastTargetPos;
        private bool _hasLastTargetPos;
        private Vector2 _targetVel;

        public void SetOrder(MoveOrder order)
        {
            _order = order;
            _wanderAngle = 0f;
            _hasLastTargetPos = false;
            _targetVel = Vector2.zero;
            _runStatePending = true;
        }

        // Replace hand-off: resume the standing order of the bot being replaced.
        public void InheritFrom(IBotAi previous)
        {
            if (previous is ManualAi manual)
                SetOrder(manual._order);
        }

        public BotIntent Decide(BotController self, float deltaTime)
        {
            if (!self.TryGetPose(out BotPose pose))
                return BotIntent.Idle; // not currently spawned; issue nothing

            BotIntent intent;
            switch (_order.Kind)
            {
                case MoveOrderKind.Seek:
                case MoveOrderKind.Arrive:
                case MoveOrderKind.Flee:
                case MoveOrderKind.Pursue:
                case MoveOrderKind.Evade:
                    if (TryResolve(_order.Target, out Vector2 target))
                        intent = MovementBehaviors.Assemble(pose, ApplyCorrectives(self, pose, PrimaryVelocity(pose, target, deltaTime)));
                    else { intent = MovementBehaviors.Stop(); _hasLastTargetPos = false; }
                    break;
                case MoveOrderKind.Wander:
                    intent = MovementBehaviors.Assemble(pose, ApplyCorrectives(self, pose, MovementBehaviors.WanderVelocity(pose, ref _wanderAngle, deltaTime)));
                    break;
                case MoveOrderKind.FacePoint:
                    intent = TryResolve(_order.Target, out Vector2 pp) ? MovementBehaviors.FacePoint(pose, pp) : MovementBehaviors.Stop();
                    break;
                case MoveOrderKind.Face:
                    intent = MovementBehaviors.Face(_order.Heading);
                    break;
                default:
                    intent = MovementBehaviors.Stop();
                    break;
            }

            // Optional decoupled facing: override only the look channel to face a separate target. If
            // unresolvable, the behavior's own (travel) facing stands.
            if (_order.FaceTarget.HasValue && TryResolve(_order.FaceTarget.Value, out Vector2 look))
                intent.LookHeading = MovementSolver.HeadingTo(pose.Position, look);

            // Establish the sticky run mode once per order, on the first tick the bot is actually spawned
            // (the not-spawned early-out above keeps it pending until then). Translating orders run.
            if (_runStatePending)
            {
                intent.Running = _order.IsTranslating;
                _runStatePending = false;
            }

            return intent;
        }

        // The primary steering velocity for the current motion order's kind.
        private Vector2 PrimaryVelocity(BotPose pose, Vector2 target, float deltaTime)
        {
            switch (_order.Kind)
            {
                case MoveOrderKind.Arrive: return Steering.Arrive(pose, target);
                case MoveOrderKind.Flee:   return Steering.Flee(pose, target);
                case MoveOrderKind.Pursue: return Steering.Pursue(pose, target, UpdateTargetVelocity(target, deltaTime));
                case MoveOrderKind.Evade:  return Steering.Evade(pose, target, UpdateTargetVelocity(target, deltaTime));
                default:                   return Steering.Seek(pose, target); // Seek
            }
        }

        // Blends the active corrective behaviors onto the primary velocity per the order's flags. With none
        // set, returns the primary unchanged (no neighbour gather / raycasts).
        private Vector2 ApplyCorrectives(BotController self, BotPose pose, Vector2 primary)
        {
            if (!_order.Separate && !_order.Avoid && !_order.Dodge)
                return primary;

            var parts = new List<(Vector2 velocity, float weight)> { (primary, 1f) };

            if (_order.Separate || _order.Dodge)
            {
                GatherNeighbours(self, out List<Vector2> positions, out List<(Vector2 pos, Vector2 vel)> withVel);
                if (_order.Separate)
                    parts.Add((Steering.Separation(pose, positions, Steering.DefaultSeparationRadius), SeparationWeight));
                if (_order.Dodge)
                {
                    CharacterTracker.TryGetVelocity(self.PlayerId, out Vector2 selfVel);
                    parts.Add((Steering.CollisionAvoidance(pose, selfVel, withVel, Steering.DefaultCollisionRadius, Steering.DefaultCollisionLookahead), DodgeWeight));
                }
            }

            if (_order.Avoid && self.Position is Vector3 p)
                parts.Add((ObstacleAvoidance.Steer(p, primary), AvoidWeight));

            return Steering.Blend(parts.ToArray());
        }

        // Neighbouring characters from the shared per-tick snapshot.
        private void GatherNeighbours(BotController self, out List<Vector2> positions, out List<(Vector2 pos, Vector2 vel)> withVel)
        {
            positions = _order.Separate ? new List<Vector2>() : null;
            withVel = _order.Dodge ? new List<(Vector2 pos, Vector2 vel)>() : null;

            IReadOnlyList<CharacterTracker.Character> characters = CharacterTracker.Characters;
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].PlayerId == self.PlayerId) continue;
                positions?.Add(characters[i].Position);
                withVel?.Add((characters[i].Position, characters[i].Velocity));
            }
        }

        // Resolves a target to a live world point. A fixed point passes through; a player target is read
        // from its current transform, or fails if that player isn't presently spawned.
        private bool TryResolve(MoveTarget target, out Vector2 point)
        {
            if (!target.IsPlayer)
            {
                point = target.Point;
                return true;
            }

            IPlayer player = StateTracker.GetPlayerById(target.PlayerId);
            if (player?.PlayerObject != null)
            {
                Vector3 wp = player.PlayerObject.transform.position;
                point = new Vector2(wp.x, wp.z);
                return true;
            }

            point = default;
            return false;
        }

        // Estimates the current target's velocity (world XZ units/sec) from its position delta since last
        // tick, EMA-smoothed. Returns zero on the first tick after (re)acquiring the target.
        private Vector2 UpdateTargetVelocity(Vector2 currentPos, float deltaTime)
        {
            if (_hasLastTargetPos && deltaTime > 0f)
            {
                Vector2 instantVel = (currentPos - _lastTargetPos) / deltaTime;
                _targetVel = Vector2.Lerp(_targetVel, instantVel, TargetVelSmoothing);
            }

            _lastTargetPos = currentPos;
            _hasLastTargetPos = true;
            return _targetVel;
        }
    }
}
