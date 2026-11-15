using System.Collections.Generic;
using UnityEngine;

namespace MDS.Shared.Game
{
    public readonly struct OverlayRow
    {
        public OverlayRow(int start, int count, bool closed)
        {
            Start = start;
            Count = count;
            Closed = closed;
        }

        public int Start { get; }

        public int Count { get; }

        public bool Closed { get; }
    }

    // Plain mesh data, so a client view is built and tested without the engine.
    // Every vertex has its own color, so one mesh carries a solid border and a fade.
    public sealed class OverlayMesh
    {
        public List<Vector3> Vertices { get; } = new();

        public List<Color> Colors { get; } = new();

        public List<int> Triangles { get; } = new();

        public OverlayRow AddRow(IReadOnlyList<Vector3> points, Color color, bool closed)
        {
            var row = new OverlayRow(Vertices.Count, points.Count, closed);
            foreach (Vector3 point in points)
            {
                Vertices.Add(point);
                Colors.Add(color);
            }
            return row;
        }

        // Two rows with the same number of points become a band of quads, wrapping round when the rows are closed.
        public void ConnectRows(OverlayRow from, OverlayRow to)
        {
            int quads = from.Closed ? from.Count : from.Count - 1;
            for (int i = 0; i < quads; i++)
            {
                int next = (i + 1) % from.Count;
                AddTriangle(from.Start + i, to.Start + i, to.Start + next);
                AddTriangle(from.Start + i, to.Start + next, from.Start + next);
            }
        }

        private void AddTriangle(int a, int b, int c)
        {
            Triangles.Add(a);
            Triangles.Add(b);
            Triangles.Add(c);
        }
    }
}
