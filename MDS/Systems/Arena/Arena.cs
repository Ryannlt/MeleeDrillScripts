using UnityEngine;

namespace MDS.Systems
{
    public class Arena
    {
        public Vector2 Corner1 { get; private set; }
        public Vector2 Corner2 { get; private set; }

        public Arena(Vector2 corner1, Vector2 corner2)
        {
            Corner1 = corner1;
            Corner2 = corner2;
        }

        public Vector2 Min => new Vector2(
            Mathf.Min(Corner1.x, Corner2.x),
            Mathf.Min(Corner1.y, Corner2.y)
        );

        public Vector2 Max => new Vector2(
            Mathf.Max(Corner1.x, Corner2.x),
            Mathf.Max(Corner1.y, Corner2.y)
        );

        public override string ToString()
        {
            return $"Arena from ({Corner1.x}, {Corner1.y}) to ({Corner2.x}, {Corner2.y})";
        }
    }
}
