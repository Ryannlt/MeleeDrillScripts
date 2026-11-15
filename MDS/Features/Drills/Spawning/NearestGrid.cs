using System;
using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Spots bucketed into cells of about one spot each once the area is full, so the nearest spot to a point is found ring by ring outward instead of through every spot.
    public sealed class NearestGrid
    {
        // At most 64 x 64 cells, whatever the count.
        private const float MostCellsPerSide = 64f;
        private const float SmallestCell = 0.01f;

        private readonly Rect _bounds;
        private readonly float _cellSize;
        private readonly int _columns;
        private readonly int _rows;
        private readonly List<Vector2>[,] _cells;

        public NearestGrid(Rect bounds, int expectedSpots)
        {
            _bounds = bounds;
            float even = Mathf.Sqrt(bounds.width * bounds.height / Mathf.Max(1, expectedSpots));
            _cellSize = Mathf.Max(even, Mathf.Max(bounds.width, bounds.height) / MostCellsPerSide, SmallestCell);
            _columns = Mathf.Max(1, Mathf.CeilToInt(bounds.width / _cellSize));
            _rows = Mathf.Max(1, Mathf.CeilToInt(bounds.height / _cellSize));
            _cells = new List<Vector2>[_columns, _rows];
        }

        public int Count { get; private set; }

        public void Add(Vector2 spot)
        {
            (int x, int y) = CellOf(spot);
            (_cells[x, y] ??= new List<Vector2>()).Add(spot);
            Count++;
        }

        // Stops as soon as the nearest is known to be under the floor, since the caller only needs to know it lost.
        // Cells in ring r lie at least (r - 1) cells away, so the search ends once that passes the nearest found.
        public float DistanceToNearest(Vector2 point, float floor)
        {
            if (Count == 0) return float.MaxValue;

            (int X, int Y) cell = CellOf(point);
            float nearest = float.MaxValue;
            int rings = Math.Max(_columns, _rows);
            for (int ring = 0; ring <= rings && nearest >= floor && (ring - 1) * _cellSize < nearest; ring++)
            {
                nearest = Mathf.Min(nearest, NearestInRing(point, cell, ring));
            }
            return nearest;
        }

        // The two end columns in full, and only the top and bottom cells of the columns between.
        private float NearestInRing(Vector2 point, (int X, int Y) cell, int ring)
        {
            float nearest = float.MaxValue;
            for (int x = cell.X - ring; x <= cell.X + ring; x++)
            {
                int step = x == cell.X - ring || x == cell.X + ring ? 1 : ring + ring;
                for (int y = cell.Y - ring; y <= cell.Y + ring; y += step) nearest = Mathf.Min(nearest, NearestInCell(point, x, y));
            }
            return nearest;
        }

        private float NearestInCell(Vector2 point, int x, int y)
        {
            if (x < 0 || y < 0 || x >= _columns || y >= _rows || _cells[x, y] == null) return float.MaxValue;

            float nearest = float.MaxValue;
            foreach (Vector2 spot in _cells[x, y]) nearest = Mathf.Min(nearest, Vector2.Distance(point, spot));
            return nearest;
        }

        private (int X, int Y) CellOf(Vector2 spot)
        {
            int x = Mathf.Clamp((int)((spot.x - _bounds.xMin) / _cellSize), 0, _columns - 1);
            int y = Mathf.Clamp((int)((spot.y - _bounds.yMin) / _cellSize), 0, _rows - 1);
            return (x, y);
        }
    }
}
