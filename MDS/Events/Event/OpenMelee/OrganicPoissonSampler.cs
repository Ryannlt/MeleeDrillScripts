using System.Collections.Generic;
using UnityEngine;

namespace MDS.Events
{
    public static class OrganicPoissonSampler
    {
        public static List<Vector2> Generate(Rect area, float spacing, int count, float offset, float maxRadiusFactor = 0.25f)
        {
            float cellSize = spacing / Mathf.Sqrt(2);
            int gridWidth = Mathf.CeilToInt(area.width / cellSize);
            int gridHeight = Mathf.CeilToInt(area.height / cellSize);

            Vector2 origin = new Vector2(area.xMin, area.yMin);
            Vector2 max = new Vector2(area.xMax, area.yMax);

            var grid = new Vector2?[gridWidth, gridHeight];
            var activeList = new List<Vector2>();
            var positions = new List<Vector2>();

            Vector2 ToGridCoords(Vector2 pos)
            {
                return new Vector2(
                    Mathf.FloorToInt((pos.x - origin.x) / cellSize),
                    Mathf.FloorToInt((pos.y - origin.y) / cellSize)
                );
            }

            bool IsValid(Vector2 point)
            {
                int gx = (int)((point.x - origin.x) / cellSize);
                int gy = (int)((point.y - origin.y) / cellSize);

                int minX = Mathf.Max(0, gx - 2);
                int maxX = Mathf.Min(gridWidth - 1, gx + 2);
                int minY = Mathf.Max(0, gy - 2);
                int maxY = Mathf.Min(gridHeight - 1, gy + 2);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (grid[x, y] != null)
                        {
                            Vector2 neighbor = grid[x, y].Value;
                            if (Vector2.Distance(neighbor, point) < spacing)
                                return false;
                        }
                    }
                }

                return true;
            }

            void AddPoint(Vector2 point)
            {
                var g = ToGridCoords(point);
                grid[(int)g.x, (int)g.y] = point;
                activeList.Add(point);
                positions.Add(point);
            }

            // Initialize with one random point
            System.Random rand = new();
            Vector2 initial = new Vector2(
                UnityEngine.Random.Range(area.xMin + offset, area.xMax - offset),
                UnityEngine.Random.Range(area.yMin + offset, area.yMax - offset)
            );
            AddPoint(initial);

            while (activeList.Count > 0 && positions.Count < count)
            {
                int idx = rand.Next(activeList.Count);
                Vector2 center = activeList[idx];
                bool found = false;

                for (int i = 0; i < 30; i++) // limit attempts per point
                {
                    float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                    float radius = UnityEngine.Random.Range(spacing, (area.width + area.height) * maxRadiusFactor);

                    Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                    if (candidate.x >= area.xMin + offset && candidate.x <= area.xMax - offset &&
                        candidate.y >= area.yMin + offset && candidate.y <= area.yMax - offset &&
                        IsValid(candidate))
                    {
                        AddPoint(candidate);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    activeList.RemoveAt(idx);
                }
            }

            return positions;
        }
    }
}
