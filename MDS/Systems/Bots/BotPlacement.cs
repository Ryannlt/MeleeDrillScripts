using UnityEngine;

namespace MDS.Systems
{
    // Where (and optionally which way) a bot should be placed on its first spawn (summon / replace).
    // Heading is degrees from North; null = leave the default facing.
    public struct BotPlacement
    {
        public Vector3 Position;
        public float? Heading;

        public BotPlacement(Vector3 position, float? heading = null)
        {
            Position = position;
            Heading = heading;
        }
    }
}
