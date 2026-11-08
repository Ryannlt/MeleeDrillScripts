using System.Collections.Generic;
using UnityEngine;

namespace MDS.Events
{
    public static class PoissonSampler
    {
        public static List<Vector2> Generate(Rect area, float radius, int maxPoints)
        {
            float cellSize = radius / Mathf.Sqrt(2);
            int gridWidth = Mathf.CeilToInt(area.width / cellSize);
            int gridHeight = Mathf.CeilToInt(area.height / cellSize);

            Vector2[,] grid = new Vector2[gridWidth, gridHeight];
            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2>();

            Vector2 firstPoint = new Vector2(
                Random.Range(area.xMin, area.xMax),
                Random.Range(area.yMin, area.yMax)
            );

            points.Add(firstPoint);
            spawnPoints.Add(firstPoint);
            InsertToGrid(firstPoint);

            while (spawnPoints.Count > 0 && points.Count < maxPoints)
            {
                int spawnIndex = Random.Range(0, spawnPoints.Count);
                Vector2 spawnCenter = spawnPoints[spawnIndex];
                bool accepted = false;

                for (int i = 0; i < 30; i++) // attempts per spawn point
                {
                    Vector2 candidate = GenerateRandomPointAround(spawnCenter, radius);
                    if (IsValid(candidate))
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        InsertToGrid(candidate);
                        accepted = true;
                        break;
                    }
                }

                if (!accepted)
                    spawnPoints.RemoveAt(spawnIndex);
            }

            return points;

            // Local Functions
            void InsertToGrid(Vector2 point)
            {
                int x = (int)((point.x - area.xMin) / cellSize);
                int y = (int)((point.y - area.yMin) / cellSize);
                grid[x, y] = point;
            }

            bool IsValid(Vector2 point)
            {
                if (!area.Contains(point))
                    return false;

                int x = (int)((point.x - area.xMin) / cellSize);
                int y = (int)((point.y - area.yMin) / cellSize);

                int xmin = Mathf.Max(0, x - 2);
                int xmax = Mathf.Min(gridWidth - 1, x + 2);
                int ymin = Mathf.Max(0, y - 2);
                int ymax = Mathf.Min(gridHeight - 1, y + 2);

                for (int i = xmin; i <= xmax; i++)
                {
                    for (int j = ymin; j <= ymax; j++)
                    {
                        if (grid[i, j] != Vector2.zero &&
                            Vector2.Distance(grid[i, j], point) < radius)
                            return false;
                    }
                }

                return true;
            }

            Vector2 GenerateRandomPointAround(Vector2 center, float radius)
            {
                float r = radius * (1 + Random.value);
                float angle = Random.value * Mathf.PI * 2;

                return new Vector2(
                    center.x + r * Mathf.Cos(angle),
                    center.y + r * Mathf.Sin(angle)
                );
            }
        }
    }
}
