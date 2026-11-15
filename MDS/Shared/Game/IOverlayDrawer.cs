using System;

namespace MDS.Shared.Game
{
    // A seam, so a client view can be tested without the engine.
    public interface IOverlayDrawer : IDisposable
    {
        void Draw(OverlayMesh mesh);

        void Clear();
    }
}
