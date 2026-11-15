using System;
using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Cells of spacing / sqrt(2) hold at most one spot each, so a new spot only needs checking against the 5 x 5 cells around its own.
    public sealed class PoissonGrid
    {
        private const int Neighborhood = 2;
        private const float CellPerSpacing = 0.70710678f;

        // About 12 MB of cells: a 350 m arena at 0.5 m spacing.
        private const long MaxCells = 1000000;

        private readonly SpotArea _area;
        private readonly float _cellSize;
        private readonly Vector2?[,] _cells;

        public PoissonGrid(SpotArea area, float spacing, float reach)
        {
            _area = area;
            Spacing = spacing;
            Reach = reach;
            _cellSize = spacing * CellPerSpacing;
            Rect bounds = area.Bounds;
            _cells = new Vector2?[Mathf.Max(1, Mathf.CeilToInt(bounds.width / _cellSize)), Mathf.Max(1, Mathf.CeilToInt(bounds.height / _cellSize))];
        }

        public float Spacing { get; }

        public float Reach { get; }

        public List<Vector2> Spots { get; } = new();

        public static bool Fits(Rect area, float spacing)
        {
            float cellSize = spacing * CellPerSpacing;
            long cells = (long)Math.Ceiling(area.width / cellSize) * (long)Math.Ceiling(area.height / cellSize);
            return cells <= MaxCells;
        }

        public bool Fits(Vector2 spot) => _area.Contains(spot) && !HasNeighborNear(spot);

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
            Rect bounds = _area.Bounds;
            int x = Mathf.Clamp((int)((spot.x - bounds.xMin) / _cellSize), 0, _cells.GetLength(0) - 1);
            int y = Mathf.Clamp((int)((spot.y - bounds.yMin) / _cellSize), 0, _cells.GetLength(1) - 1);
            return (x, y);
        }
    }
}
