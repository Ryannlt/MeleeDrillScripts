using System.Collections.Generic;
using MDS.Shared.Logging;
using UnityEngine;
using UnityEngine.Rendering;

namespace MDS.Shared.Game
{
    // Sprites/Default is one of Unity's always-included shaders, blends by vertex alpha and draws both faces, so no shader of our own is needed.
    public sealed class UnityOverlayDrawer : IOverlayDrawer
    {
        private const string RootName = "MDS Overlay";
        private const string ShaderName = "Sprites/Default";

        // Past this a mesh needs 32-bit indices.
        private const int MaxShortIndexVertices = 65535;

        private readonly ModLog _log;
        private readonly List<Mesh> _meshes = new();
        private GameObject _root;
        private Material _material;

        public UnityOverlayDrawer(ModLog log)
        {
            _log = log;
        }

        public void Draw(OverlayMesh overlay)
        {
            if (!TryGetMaterial(out Material material)) return;

            var piece = new GameObject("Overlay");
            piece.transform.SetParent(Root(), false);
            piece.AddComponent<MeshFilter>().sharedMesh = MeshOf(overlay);
            var renderer = piece.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Meshes made in code outlive their GameObject unless destroyed themselves.
        public void Clear()
        {
            foreach (Mesh mesh in _meshes) Object.Destroy(mesh);
            _meshes.Clear();
            if (_root != null) Object.Destroy(_root);
            _root = null;
        }

        public void Dispose()
        {
            Clear();
            if (_material != null) Object.Destroy(_material);
            _material = null;
        }

        private Mesh MeshOf(OverlayMesh overlay)
        {
            var mesh = new Mesh { indexFormat = overlay.Vertices.Count > MaxShortIndexVertices ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(overlay.Vertices);
            mesh.SetColors(overlay.Colors);
            mesh.SetTriangles(overlay.Triangles, 0);
            mesh.RecalculateBounds();
            _meshes.Add(mesh);
            return mesh;
        }

        private Transform Root()
        {
            if (_root == null) _root = new GameObject(RootName);
            return _root.transform;
        }

        private bool TryGetMaterial(out Material material)
        {
            if (_material == null)
            {
                Shader shader = Shader.Find(ShaderName);
                if (shader != null) _material = new Material(shader);
                else _log.Error($"{ShaderName} isn't loaded, so nothing is drawn.");
            }

            material = _material;
            return material != null;
        }
    }
}
