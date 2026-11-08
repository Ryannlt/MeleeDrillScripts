using UnityEngine;

namespace MDS.Systems
{
    public enum MoveOrderKind { Stop, Seek, Arrive, Flee, Pursue, Evade, Face, FacePoint, Wander }

    // A destination for a point-based movement order: either a fixed point or a tracked player.
    public struct MoveTarget
    {
        public bool IsPlayer;
        public int PlayerId;      // when IsPlayer
        public Vector2 Point;     // when !IsPlayer

        public static MoveTarget At(Vector2 point) => new MoveTarget { Point = point };
        public static MoveTarget Player(int playerId) => new MoveTarget { IsPlayer = true, PlayerId = playerId };
    }

    // A single movement instruction for the Manual test AI.
    public struct MoveOrder
    {
        public MoveOrderKind Kind;
        public MoveTarget Target;        // Seek / Arrive / Flee / Pursue / Evade / FacePoint
        public float Heading;            // Face: degrees from North
        public MoveTarget? FaceTarget;   // optional decoupled facing for translating orders (null means face travel direction)
        public bool Separate;            // blend in Separation (spread apart from nearby bots)
        public bool Avoid;               // blend in Obstacle Avoidance (steer around walls)
        public bool Dodge;               // blend in Collision Avoidance (steer around moving agents)

        public static MoveOrder Stop() => new MoveOrder { Kind = MoveOrderKind.Stop };
        public static MoveOrder Seek(MoveTarget target) => new MoveOrder { Kind = MoveOrderKind.Seek, Target = target };
        public static MoveOrder Arrive(MoveTarget target) => new MoveOrder { Kind = MoveOrderKind.Arrive, Target = target };
        public static MoveOrder Flee(MoveTarget target) => new MoveOrder { Kind = MoveOrderKind.Flee, Target = target };
        public static MoveOrder Pursue(MoveTarget target) => new MoveOrder { Kind = MoveOrderKind.Pursue, Target = target };
        public static MoveOrder Evade(MoveTarget target) => new MoveOrder { Kind = MoveOrderKind.Evade, Target = target };
        public static MoveOrder FacePoint(MoveTarget target) => new MoveOrder { Kind = MoveOrderKind.FacePoint, Target = target };
        public static MoveOrder Face(float heading) => new MoveOrder { Kind = MoveOrderKind.Face, Heading = heading };
        public static MoveOrder Wander() => new MoveOrder { Kind = MoveOrderKind.Wander };

        // Whether carrying out this order involves translation (so the actuator should enable running).
        public bool IsTranslating =>
            Kind == MoveOrderKind.Seek || Kind == MoveOrderKind.Arrive || Kind == MoveOrderKind.Flee ||
            Kind == MoveOrderKind.Pursue || Kind == MoveOrderKind.Evade || Kind == MoveOrderKind.Wander;
    }
}
