using System;
using MDS.Shared.Game;
using MDS.Shared.Logging;

namespace MDS.Shared.Runtime
{
    // Everything that needs the engine, made as one set, so a game clock never ends up beside a test log sink.
    public interface IRuntime
    {
        ILogSink CreateLogSink();

        IClock CreateClock();

        IGround CreateGround(ModLog log);

        IOverlayDrawer CreateOverlayDrawer(ModLog log);

        Random CreateRandom();
    }
}
