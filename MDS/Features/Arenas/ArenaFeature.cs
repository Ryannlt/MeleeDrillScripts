using System;

namespace MDS.Features.Arenas
{
    public sealed class ArenaFeature : IDisposable
    {
        public ArenaFeature(ModContext context)
        {
            Store = new ArenaStore(context.Log.For("Arena"));
            context.Commands.Register(new ArenaCommand(Store, context.Queries, context.Replies));
            context.Config.Register(new ArenaVariable(Store));
            context.Config.Register(new AddArenaVariable(Store));
            context.Config.Register(new ArenaCornerVariable(ArenaCorner.Corner1, Store));
            context.Config.Register(new ArenaCornerVariable(ArenaCorner.Corner2, Store));
        }

        public ArenaStore Store { get; }

        // It subscribes to nothing, so there is nothing to release.
        public void Dispose()
        {
        }
    }
}
