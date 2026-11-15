using System;
using MDS.Shared.Logging;

namespace MDS.Features.Arenas
{
    public sealed class ArenaFeature : IDisposable
    {
        private readonly ArenaViewers _viewers;
        private readonly ArenaOverlay _overlay;

        public ArenaFeature(ModContext context)
        {
            ModLog log = context.Log.For("Arena");
            Store = new ArenaStore(log);
            _viewers = new ArenaViewers(Store, context.Players, context.Replies);
            context.Commands.Register(new ArenaCommand(Store, _viewers, context));
            context.Config.Register(new ArenaVariable(Store));
            context.Config.Register(new ArenaCornerVariable(ArenaCorner.Corner1, Store));
            context.Config.Register(new ArenaCornerVariable(ArenaCorner.Corner2, Store));
            if (context.IsServer) return;

            _overlay = new ArenaOverlay(context.Runtime.CreateOverlayDrawer(log), context.Runtime.CreateGround(log));
            context.ClientMessages.Register(ArenaViewMessage.Name, _overlay.OnMessage);
        }

        public ArenaStore Store { get; }

        public void Dispose()
        {
            _viewers.Dispose();
            _overlay?.Dispose();
        }
    }
}
