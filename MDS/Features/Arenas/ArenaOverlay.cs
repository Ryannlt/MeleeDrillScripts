using System;
using System.Collections.Generic;
using System.Globalization;
using MDS.Shared.Game;
using UnityEngine;

namespace MDS.Features.Arenas
{
    // The client half: it paints what the server sent this player on the terrain and decides nothing.
    public sealed class ArenaOverlay : IDisposable
    {
        // A point every meter follows the terrain, and the lift keeps the paint from sinking into it between points.
        private const float PointSpacing = 1f;
        private const float Lift = 0.1f;

        private const float BorderWidth = 0.12f;
        private const float FadeWidth = 1.5f;
        private const float FadeStartAlpha = 0.35f;

        // The number in the middle is a quarter of the arena's shorter side, within these bounds.
        private const float NumberShare = 0.25f;
        private const float MinNumberHeight = 1.5f;
        private const float MaxNumberHeight = 4f;
        private const float StrokeShare = 0.15f;
        private const float DigitGap = 0.5f;
        private const float NumberAlpha = 0.6f;

        private const float Half = 0.5f;
        private const int Sides = 4;

        private readonly IOverlayDrawer _drawer;
        private readonly IGround _ground;

        public ArenaOverlay(IOverlayDrawer drawer, IGround ground)
        {
            _drawer = drawer;
            _ground = ground;
        }

        public void OnMessage(byte[] data)
        {
            _drawer.Clear();
            foreach (Arena arena in ArenaViewMessage.Decode(data)) _drawer.Draw(MeshOf(arena));
        }

        public void Dispose() => _drawer.Dispose();

        private static Color Faded(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private static Vector2[] CornersOf(Vector2 min, Vector2 max) => new[] { min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y) };

        // Every inset gets the outer edges' point counts, so its row lines up with the others.
        private static int[] StepsPerSide(Arena arena)
        {
            Vector2[] corners = CornersOf(arena.Min, arena.Max);
            var steps = new int[Sides];
            for (int i = 0; i < Sides; i++) steps[i] = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(corners[i], corners[(i + 1) % Sides]) / PointSpacing));
            return steps;
        }

        private OverlayMesh MeshOf(Arena arena)
        {
            var mesh = new OverlayMesh();
            Color color = ArenaColors.ColorOf(arena.Number);
            AddFrame(mesh, arena, color);
            AddNumber(new Pen(mesh, Faded(color, NumberAlpha), NumberHeight(arena) * StrokeShare), arena);
            return mesh;
        }

        private void AddFrame(OverlayMesh mesh, Arena arena, Color color)
        {
            int[] steps = StepsPerSide(arena);
            List<Vector3> borderInside = Ring(arena, BorderWidth, steps);
            mesh.ConnectRows(mesh.AddRow(Ring(arena, 0f, steps), color, true), mesh.AddRow(borderInside, color, true));
            OverlayRow fadeStart = mesh.AddRow(borderInside, Faded(color, FadeStartAlpha), true);
            mesh.ConnectRows(fadeStart, mesh.AddRow(Ring(arena, BorderWidth + FadeWidth, steps), Faded(color, 0f), true));
        }

        // The arena's edge moved in by the inset, which stops at the middle of a small arena.
        private List<Vector3> Ring(Arena arena, float inset, int[] steps)
        {
            Vector2 size = arena.Max - arena.Min;
            float clamped = Mathf.Min(inset, Mathf.Min(size.x, size.y) * Half);
            Vector2[] corners = CornersOf(arena.Min + Vector2.one * clamped, arena.Max - Vector2.one * clamped);
            var points = new List<Vector3>();
            for (int i = 0; i < Sides; i++) points.AddRange(Edge(corners[i], corners[(i + 1) % Sides], steps[i]));
            return points;
        }

        // The edge's end is the next edge's start, so it is left out here.
        private List<Vector3> Edge(Vector2 from, Vector2 to, int steps)
        {
            var points = new List<Vector3>(steps);
            for (int step = 0; step < steps; step++) points.Add(OnTerrain(Vector2.Lerp(from, to, (float)step / steps)));
            return points;
        }

        private float NumberHeight(Arena arena)
        {
            Vector2 size = arena.Max - arena.Min;
            return Mathf.Clamp(Mathf.Min(size.x, size.y) * NumberShare, MinNumberHeight, MaxNumberHeight);
        }

        // Centered on the arena, reading left to right when facing north.
        private void AddNumber(Pen pen, Arena arena)
        {
            string text = arena.Number.ToString(CultureInfo.InvariantCulture);
            float scale = NumberHeight(arena) / SevenSegment.CellHeight;
            float advance = (SevenSegment.CellWidth + DigitGap) * scale;
            float width = text.Length * advance - DigitGap * scale;
            Vector2 origin = arena.Center - new Vector2(width, SevenSegment.CellHeight * scale) * Half;
            for (int i = 0; i < text.Length; i++)
            {
                Vector2 cell = origin + Vector2.right * (i * advance);
                foreach (var stroke in SevenSegment.StrokesOf(text[i])) Stroke(pen, cell + stroke.From * scale, cell + stroke.To * scale);
            }
        }

        private void Stroke(Pen pen, Vector2 from, Vector2 to)
        {
            Vector2 side = Vector2.Perpendicular((to - from).normalized) * (pen.Width * Half);
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / PointSpacing));
            OverlayRow left = pen.Mesh.AddRow(Line(from + side, to + side, steps), pen.Color, false);
            pen.Mesh.ConnectRows(left, pen.Mesh.AddRow(Line(from - side, to - side, steps), pen.Color, false));
        }

        private List<Vector3> Line(Vector2 from, Vector2 to, int steps)
        {
            var points = new List<Vector3>(steps + 1);
            for (int step = 0; step <= steps; step++) points.Add(OnTerrain(Vector2.Lerp(from, to, (float)step / steps)));
            return points;
        }

        // The terrain only: a ray would put the paint on top of fences and walls along the edge.
        private Vector3 OnTerrain(Vector2 spot) => new Vector3(spot.x, _ground.TerrainHeightAt(spot) + Lift, spot.y);

        private sealed class Pen
        {
            public Pen(OverlayMesh mesh, Color color, float width)
            {
                Mesh = mesh;
                Color = color;
                Width = width;
            }

            public OverlayMesh Mesh { get; }

            public Color Color { get; }

            public float Width { get; }
        }
    }
}
