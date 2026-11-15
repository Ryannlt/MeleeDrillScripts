using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Grows outward from one random spot, so players end up in uneven pockets and fights start at once.
    public sealed class OrganicGenerator : ISpotGenerator
    {
        private const int AttemptsPerSpot = 30;

        // A reach of a quarter of the area's width plus height scatters the spots instead of packing them.
        private const float ReachPerAreaSize = 0.25f;
        private const float FullTurn = 2f * Mathf.PI;

        private readonly System.Random _random;

        public OrganicGenerator(System.Random random)
        {
            _random = random;
        }

        public List<Vector2> Generate(SpotArea area, float spacing, int count)
        {
            if (count <= 0 || !area.TryDraw(_random, out Vector2 first)) return new List<Vector2>();

            float reach = Mathf.Max(spacing, (area.Bounds.width + area.Bounds.height) * ReachPerAreaSize);
            var grid = new PoissonGrid(area, spacing, reach);
            grid.Add(first);
            return Grow(grid, count);
        }

        private List<Vector2> Grow(PoissonGrid grid, int count)
        {
            var active = new List<Vector2>(grid.Spots);
            while (active.Count > 0 && grid.Spots.Count < count)
            {
                int index = _random.Next(active.Count);
                if (TryGrowFrom(active[index], grid, out Vector2 spot))
                {
                    grid.Add(spot);
                    active.Add(spot);
                }
                else active.RemoveAt(index);
            }
            return grid.Spots;
        }

        private bool TryGrowFrom(Vector2 center, PoissonGrid grid, out Vector2 spot)
        {
            for (int attempt = 0; attempt < AttemptsPerSpot; attempt++)
            {
                float angle = (float)(_random.NextDouble() * FullTurn);
                float radius = grid.Spacing + (grid.Reach - grid.Spacing) * (float)_random.NextDouble();
                spot = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (grid.Fits(spot)) return true;
            }

            spot = default;
            return false;
        }
    }
}
