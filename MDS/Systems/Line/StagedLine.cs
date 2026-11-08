using UnityEngine;

namespace MDS.Systems
{
    // A bot line queued by a SpawnLine config variable, waiting for the round to start.
    public struct StagedLine
    {
        public Vector2 Center;        // world (x,z) the line is centred on
        public float Rotation;        // degrees from North that every bot in the line faces
        public LineSpec Spec;

        public StagedLine(Vector2 center, float rotation, LineSpec spec)
        {
            Center = center;
            Rotation = rotation;
            Spec = spec;
        }
    }
}
