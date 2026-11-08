using UnityEngine;

namespace MDS.Systems
{
    // A planar snapshot of a bot's pose, taken fresh each tick. XZ only; Y is not part of the movement model.
    public struct BotPose
    {
        public Vector2 Position;   // world XZ
        public float Heading;      // degrees from North

        public BotPose(Vector2 position, float heading)
        {
            Position = position;
            Heading = heading;
        }
    }
}
