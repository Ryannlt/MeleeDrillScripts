using System;
using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills
{
    // Bridson's Poisson disc sampling, grown outward from a random first spot.
    public sealed class PoissonSampler
    {
        private const int AttemptsPerSpot = 30;
        // A reach of a quarter of the area's width plus height scatters the spots instead of packing them.
        private const float ReachPerAreaSize = 0.25f;
        private const double FullTurn = 2 * Math.PI;

        private readonly System.Random _random;

        public PoissonSampler(System.Random random)
        {
            _random = random;
        }

        // Fewer than `count` spots when the area runs out of room.
        public List<Vector2> Generate(Rect area, float spacing, int count)
        {
            float reach = Mathf.Max(spacing, (area.width + area.height) * ReachPerAreaSize);
            var grid = new PoissonGrid(area, spacing, reach);
            var active = new List<Vector2> { RandomSpotIn(area) };
            grid.Add(active[0]);

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
                float radius = Between(grid.Spacing, grid.Reach);
                spot = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                if (grid.Fits(spot)) return true;
            }

            spot = default;
            return false;
        }

        private Vector2 RandomSpotIn(Rect area) => new Vector2(Between(area.xMin, area.xMax), Between(area.yMin, area.yMax));

        private float Between(float from, float to) => from + (to - from) * (float)_random.NextDouble();
    }

    // Cells of spacing / sqrt(2) hold at most one spot each, so a new spot only needs checking against the 5 x 5 cells around its own.
    public sealed class PoissonGrid
    {
        private const int Neighborhood = 2;
        private const float CellPerSpacing = 0.70710678f;

        // About 12 MB of cells: a 350 m arena at 0.5 m spacing.
        private const long MaxCells = 1000000;

        private readonly Rect _area;
        private readonly float _cellSize;
        private readonly Vector2?[,] _cells;

        public PoissonGrid(Rect area, float spacing, float reach)
        {
            _area = area;
            Spacing = spacing;
            Reach = reach;
            _cellSize = spacing * CellPerSpacing;
            _cells = new Vector2?[Mathf.Max(1, Mathf.CeilToInt(area.width / _cellSize)), Mathf.Max(1, Mathf.CeilToInt(area.height / _cellSize))];
        }

        public float Spacing { get; }

        public float Reach { get; }

        public static bool Fits(Rect area, float spacing)
        {
            float cellSize = spacing * CellPerSpacing;
            long cells = (long)Math.Ceiling(area.width / cellSize) * (long)Math.Ceiling(area.height / cellSize);
            return cells <= MaxCells;
        }

        public List<Vector2> Spots { get; } = new();

        public bool Fits(Vector2 spot) => spot.x >= _area.xMin && spot.x <= _area.xMax && spot.y >= _area.yMin && spot.y <= _area.yMax && !HasNeighborNear(spot);

        public void Add(Vector2 spot)
        {
            (int x, int y) = CellOf(spot);
            _cells[x, y] = spot;
            Spots.Add(spot);
        }

        private bool HasNeighborNear(Vector2 spot)
        {
            (int cx, int cy) = CellOf(spot);
            int lastX = Math.Min(_cells.GetLength(0) - 1, cx + Neighborhood);
            for (int x = Math.Max(0, cx - Neighborhood); x <= lastX; x++)
            {
                if (ColumnHasNeighborNear(x, cy, spot)) return true;
            }
            return false;
        }

        private bool ColumnHasNeighborNear(int x, int cy, Vector2 spot)
        {
            int lastY = Math.Min(_cells.GetLength(1) - 1, cy + Neighborhood);
            for (int y = Math.Max(0, cy - Neighborhood); y <= lastY; y++)
            {
                if (_cells[x, y] is Vector2 other && Vector2.Distance(other, spot) < Spacing) return true;
            }
            return false;
        }

        private (int X, int Y) CellOf(Vector2 spot)
        {
            int x = Mathf.Clamp((int)((spot.x - _area.xMin) / _cellSize), 0, _cells.GetLength(0) - 1);
            int y = Mathf.Clamp((int)((spot.y - _area.yMin) / _cellSize), 0, _cells.GetLength(1) - 1);
            return (x, y);
        }
    }
}
