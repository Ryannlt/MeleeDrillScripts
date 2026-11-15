using System;
using MDS.Shared.Game;
using MDS.Shared.Logging;

namespace MDS.Shared.Runtime
{
    public sealed class UnityRuntime : IRuntime
    {
        public ILogSink CreateLogSink() => new UnityLogSink();

        public IClock CreateClock() => new GameClock();

        public IGround CreateGround(ModLog log) => new TerrainGround(log);

        public IOverlayDrawer CreateOverlayDrawer(ModLog log) => new UnityOverlayDrawer(log);

        public Random CreateRandom() => new Random();
    }
}
