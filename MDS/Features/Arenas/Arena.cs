using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Arenas
{
    // The rectangle drills place players in, from two opposite corners on the map (x, z).
    public sealed class Arena
    {
        private const float Midway = 0.5f;

        public Arena(Vector2 corner1, Vector2 corner2)
        {
            Corner1 = corner1;
            Corner2 = corner2;
        }

        public Vector2 Corner1 { get; }

        public Vector2 Corner2 { get; }

        public Vector2 Min => Vector2.Min(Corner1, Corner2);

        public Vector2 Max => Vector2.Max(Corner1, Corner2);

        public Vector2 Center => Vector2.Lerp(Corner1, Corner2, Midway);

        // Corners apart on both axes, so there is room to place anyone.
        public bool IsUsable => Min.x < Max.x && Min.y < Max.y;

        public override string ToString() => $"{Format(Corner1)} to {Format(Corner2)}";

        public static string Format(Vector2 corner) => $"({Numbers.Coordinate(corner.x)}, {Numbers.Coordinate(corner.y)})";
    }
}
