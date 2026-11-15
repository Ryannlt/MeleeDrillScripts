using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills
{
    public sealed class LineShape
    {
        public Vector2 Center { get; set; }

        public float Distance { get; set; }

        public float Spacing { get; set; }

        // The compass direction from the attackers' line toward the defenders'.
        public float Degrees { get; set; }

        // Degrees was drawn at random, so the next run faces another way.
        public bool FacingIsRandom { get; set; }

        public LineShape Resized(float distance, float spacing)
        {
            return new LineShape { Center = Center, Distance = distance, Spacing = spacing, Degrees = Degrees, FacingIsRandom = FacingIsRandom };
        }
    }

    public static class LinePlacement
    {
        private const float AttackerSide = -1f;
        private const float DefenderSide = 1f;
        private const float Half = 0.5f;

        public static List<Vector2> AttackerSpots(LineShape shape, int count) => Spots(shape, count, AttackerSide);

        public static List<Vector2> DefenderSpots(LineShape shape, int count) => Spots(shape, count, DefenderSide);

        private static List<Vector2> Spots(LineShape shape, int count, float side)
        {
            float radians = shape.Degrees * Mathf.Deg2Rad;
            var forward = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            var along = new Vector2(-forward.y, forward.x);
            Vector2 lineCenter = shape.Center + forward * (side * shape.Distance * Half);
            float first = -(count - 1) * shape.Spacing * Half;

            var spots = new List<Vector2>(count);
            for (int i = 0; i < count; i++) spots.Add(lineCenter + along * (first + i * shape.Spacing));
            return spots;
        }
    }
}
