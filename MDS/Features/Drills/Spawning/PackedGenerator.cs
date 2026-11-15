using System;
using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Spots as tight as the spacing allows: the points of a honeycomb, turned a random way, nearest first to a random spot in the area.
    // A comb that comes up short is laid along the area's long side instead, so a thin area still takes a row.
    public sealed class PackedGenerator : ISpotGenerator
    {
        private const float FullTurn = 2f * Mathf.PI;

        // A honeycomb's rows sit sqrt(3)/2 of the spacing apart, and every other row shifts half a spacing.
        private const float RowShare = 0.8660254f;
        private const float Half = 0.5f;
        private const int EveryOtherRow = 2;
        private const float Quarter = 0.25f;

        private readonly System.Random _random;

        public PackedGenerator(System.Random random)
        {
            _random = random;
        }

        // The honeycomb is built only as far out as the count needs, and twice as far each time the area cuts it short.
        public List<Vector2> Generate(SpotArea area, float spacing, int count)
        {
            if (count <= 0 || !area.TryDraw(_random, out Vector2 anchor)) return new List<Vector2>();

            List<Vector2> spots = Fill(new Honeycomb(anchor, spacing, (float)(_random.NextDouble() * FullTurn)), area, count);
            if (spots.Count >= count) return spots;

            float alongLongSide = area.Bounds.width >= area.Bounds.height ? 0f : FullTurn * Quarter;
            List<Vector2> aligned = Fill(new Honeycomb(anchor, spacing, alongLongSide), area, count);
            return aligned.Count > spots.Count ? aligned : spots;
        }

        private static List<Vector2> Fill(Honeycomb comb, SpotArea area, int count)
        {
            float farthest = new Vector2(area.Bounds.width, area.Bounds.height).magnitude + comb.Spacing;
            for (float reach = comb.Spacing * (Mathf.Sqrt(count) + 1f); ; reach += reach)
            {
                List<Vector2> spots = comb.PointsWithin(reach, area);
                if (spots.Count >= count || reach >= farthest) return spots.GetRange(0, Math.Min(count, spots.Count));
            }
        }

        private sealed class Honeycomb
        {
            private readonly Vector2 _anchor;
            private readonly Vector2 _right;
            private readonly Vector2 _up;

            public Honeycomb(Vector2 anchor, float spacing, float angle)
            {
                _anchor = anchor;
                Spacing = spacing;
                _right = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _up = new Vector2(-_right.y, _right.x);
            }

            public float Spacing { get; }

            // Only those in the area, nearest to the anchor first.
            public List<Vector2> PointsWithin(float reach, SpotArea area)
            {
                var points = new List<Vector2>();
                int rows = Mathf.CeilToInt(reach / (Spacing * RowShare));
                for (int row = -rows; row <= rows; row++) AddRow(row, reach, points);
                points = points.FindAll(area.Contains);
                points.Sort((a, b) => (a - _anchor).sqrMagnitude.CompareTo((b - _anchor).sqrMagnitude));
                return points;
            }

            private void AddRow(int row, float reach, List<Vector2> points)
            {
                int columns = Mathf.CeilToInt(reach / Spacing) + 1;
                float shift = Mathf.Abs(row) % EveryOtherRow == 1 ? Spacing * Half : 0f;
                for (int column = -columns; column <= columns; column++)
                {
                    var local = new Vector2(column * Spacing + shift, row * Spacing * RowShare);
                    if (local.magnitude <= reach) points.Add(_anchor + _right * local.x + _up * local.y);
                }
            }
        }
    }
}
