using System;
using MDS.Shared.Game;
using MDS.Shared.Logging;

namespace MDS.Shared.Runtime
{
    // Abstract Factory for everything that needs the engine. The game uses UnityRuntime and the unit tests their own,
    // and each makes the whole set, so a game clock never ends up beside a test log sink.
    public interface IRuntime
    {
        ILogSink CreateLogSink();

        IClock CreateClock();

        IGround CreateGround(ModLog log);

        Random CreateRandom();
    }
}
