using System;
using System.Collections.Generic;
using MDS.Shared.Game;
using UnityEngine;

namespace MDS.Features.Drills.Preview
{
    // The client half of rc preview: rings where the drill would put players, and the area open melee draws from.
    public sealed class SpotPreviewOverlay : IDisposable
    {
        // A ring about a player wide.
        private const float RingRadius = 0.4f;
        private const float RingWidth = 0.12f;
        private const int RingPoints = 12;

        private const float AreaBorderWidth = 0.08f;
        private const float AreaAlpha = 0.6f;

        private static readonly Color AttackerColor = Color.yellow;
        private static readonly Color DefenderColor = Color.cyan;

        private readonly IOverlayDrawer _drawer;
        private readonly GroundBrush _brush;

        public SpotPreviewOverlay(IOverlayDrawer drawer, IGround ground)
        {
            _drawer = drawer;
            _brush = new GroundBrush(ground);
        }

        public void OnMessage(byte[] data)
        {
            _drawer.Clear();
            if (SpotViewMessage.TryDecode(data, out DrillPreview preview)) _drawer.Draw(MeshOf(preview));
        }

        public void Dispose() => _drawer.Dispose();

        private OverlayMesh MeshOf(DrillPreview preview)
        {
            var mesh = new OverlayMesh();
            if (preview.Area.width > 0f) AddAreaBorder(mesh, preview.Area);
            foreach (Vector2 spot in preview.Attackers) AddRing(mesh, spot, AttackerColor);
            foreach (Vector2 spot in preview.Defenders) AddRing(mesh, spot, DefenderColor);
            return mesh;
        }

        private void AddAreaBorder(OverlayMesh mesh, Rect area)
        {
            int[] steps = GroundBrush.StepsPerSide(area);
            var color = new Color(Color.white.r, Color.white.g, Color.white.b, AreaAlpha);
            mesh.ConnectRows(mesh.AddRow(_brush.RectRing(area, 0f, steps), color, true), mesh.AddRow(_brush.RectRing(area, AreaBorderWidth, steps), color, true));
        }

        private void AddRing(OverlayMesh mesh, Vector2 spot, Color color)
        {
            List<Vector3> outer = _brush.Circle(spot, RingRadius, RingPoints);
            mesh.ConnectRows(mesh.AddRow(outer, color, true), mesh.AddRow(_brush.Circle(spot, RingRadius - RingWidth, RingPoints), color, true));
        }
    }
}
