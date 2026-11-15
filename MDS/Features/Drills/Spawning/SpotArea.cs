using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // A box, or one side of it past a line through its middle.
    public sealed class SpotArea
    {
        private const int MaxDraws = 100;
        private const float HalfOfSide = 0.5f;

        private readonly Vector2 _toward;
        private readonly float _gap;

        private SpotArea(Rect bounds, Vector2 toward, float gap)
        {
            Bounds = bounds;
            _toward = toward;
            _gap = gap;
        }

        public Rect Bounds { get; }

        public static SpotArea Whole(Rect bounds) => new SpotArea(bounds, Vector2.zero, float.NegativeInfinity);

        // The side the direction points to, starting the gap's distance past the middle line.
        public static SpotArea Half(Rect bounds, Vector2 toward, float gap) => new SpotArea(bounds, toward.normalized, gap);

        // Moved in by the margin on every side; a box too small for that becomes its middle line or point.
        public static Rect Shrunk(Rect area, float margin)
        {
            float x = area.width > margin + margin ? margin : area.width * HalfOfSide;
            float y = area.height > margin + margin ? margin : area.height * HalfOfSide;
            return Rect.MinMaxRect(area.xMin + x, area.yMin + y, area.xMax - x, area.yMax - y);
        }

        public bool Contains(Vector2 spot)
        {
            bool inBounds = spot.x >= Bounds.xMin && spot.x <= Bounds.xMax && spot.y >= Bounds.yMin && spot.y <= Bounds.yMax;
            return inBounds && Vector2.Dot(spot - Bounds.center, _toward) >= _gap;
        }

        // False when the area is too thin to hit, such as a half whose gap is wider than the box.
        public bool TryDraw(System.Random random, out Vector2 spot)
        {
            for (int draw = 0; draw < MaxDraws; draw++)
            {
                spot = new Vector2(random.Between(Bounds.xMin, Bounds.xMax), random.Between(Bounds.yMin, Bounds.yMax));
                if (Contains(spot)) return true;
            }

            spot = default;
            return false;
        }
    }
}
