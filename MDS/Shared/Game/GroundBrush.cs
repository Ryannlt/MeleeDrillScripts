using System.Collections.Generic;
using UnityEngine;

namespace MDS.Shared.Game
{
    // Points for paint on the ground: a point every meter follows the terrain, and the lift keeps the paint from sinking into it between points.
    // The terrain only, since a ray would put the paint on top of fences and walls.
    public sealed class GroundBrush
    {
        public const float PointSpacing = 1f;

        private const float Lift = 0.1f;
        private const float Half = 0.5f;
        private const int Sides = 4;
        private const float FullTurn = 2f * Mathf.PI;

        private readonly IGround _ground;

        public GroundBrush(IGround ground)
        {
            _ground = ground;
        }

        public static int StepsAlong(Vector2 from, Vector2 to) => Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / PointSpacing));

        // Every inset of one rectangle gets these point counts, so its rows line up.
        public static int[] StepsPerSide(Rect rect)
        {
            Vector2[] corners = CornersOf(rect.min, rect.max);
            var steps = new int[Sides];
            for (int i = 0; i < Sides; i++) steps[i] = StepsAlong(corners[i], corners[(i + 1) % Sides]);
            return steps;
        }

        public Vector3 OnTerrain(Vector2 spot) => new Vector3(spot.x, _ground.TerrainHeightAt(spot) + Lift, spot.y);

        // The rectangle's edge moved in by the inset, which stops at the middle of a small rectangle.
        public List<Vector3> RectRing(Rect rect, float inset, int[] steps)
        {
            float clamped = Mathf.Min(inset, Mathf.Min(rect.width, rect.height) * Half);
            Vector2[] corners = CornersOf(rect.min + Vector2.one * clamped, rect.max - Vector2.one * clamped);
            var points = new List<Vector3>();
            for (int i = 0; i < Sides; i++) points.AddRange(Edge(corners[i], corners[(i + 1) % Sides], steps[i]));
            return points;
        }

        // Both ends included.
        public List<Vector3> Line(Vector2 from, Vector2 to, int steps)
        {
            var points = new List<Vector3>(steps + 1);
            for (int step = 0; step <= steps; step++) points.Add(OnTerrain(Vector2.Lerp(from, to, (float)step / steps)));
            return points;
        }

        public List<Vector3> Circle(Vector2 center, float radius, int points)
        {
            var circle = new List<Vector3>(points);
            for (int i = 0; i < points; i++)
            {
                float angle = FullTurn * i / points;
                circle.Add(OnTerrain(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius));
            }
            return circle;
        }

        private static Vector2[] CornersOf(Vector2 min, Vector2 max) => new[] { min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y) };

        // The edge's end is the next edge's start, so it is left out here.
        private List<Vector3> Edge(Vector2 from, Vector2 to, int steps)
        {
            var points = new List<Vector3>(steps);
            for (int step = 0; step < steps; step++) points.Add(OnTerrain(Vector2.Lerp(from, to, (float)step / steps)));
            return points;
        }
    }
}
