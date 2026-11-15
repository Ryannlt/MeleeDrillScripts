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

        private readonly IOverlayDrawer _drawer;
        private readonly GroundBrush _brush;

        public ArenaOverlay(IOverlayDrawer drawer, IGround ground)
        {
            _drawer = drawer;
            _brush = new GroundBrush(ground);
        }

        public void OnMessage(byte[] data)
        {
            _drawer.Clear();
            foreach (Arena arena in ArenaViewMessage.Decode(data)) _drawer.Draw(MeshOf(arena));
        }

        public void Dispose() => _drawer.Dispose();

        private static Color Faded(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private static float NumberHeight(Arena arena)
        {
            Vector2 size = arena.Max - arena.Min;
            return Mathf.Clamp(Mathf.Min(size.x, size.y) * NumberShare, MinNumberHeight, MaxNumberHeight);
        }

        private OverlayMesh MeshOf(Arena arena)
        {
            var mesh = new OverlayMesh();
            Color color = ArenaColors.ColorOf(arena.Number);
            AddFrame(mesh, arena.Bounds, color);
            AddNumber(new Pen(mesh, Faded(color, NumberAlpha), NumberHeight(arena) * StrokeShare), arena);
            return mesh;
        }

        private void AddFrame(OverlayMesh mesh, Rect rect, Color color)
        {
            int[] steps = GroundBrush.StepsPerSide(rect);
            List<Vector3> borderInside = _brush.RectRing(rect, BorderWidth, steps);
            mesh.ConnectRows(mesh.AddRow(_brush.RectRing(rect, 0f, steps), color, true), mesh.AddRow(borderInside, color, true));
            OverlayRow fadeStart = mesh.AddRow(borderInside, Faded(color, FadeStartAlpha), true);
            mesh.ConnectRows(fadeStart, mesh.AddRow(_brush.RectRing(rect, BorderWidth + FadeWidth, steps), Faded(color, 0f), true));
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
            int steps = GroundBrush.StepsAlong(from, to);
            OverlayRow left = pen.Mesh.AddRow(_brush.Line(from + side, to + side, steps), pen.Color, false);
            pen.Mesh.ConnectRows(left, pen.Mesh.AddRow(_brush.Line(from - side, to - side, steps), pen.Color, false));
        }

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
