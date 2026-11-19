using MDS.Features.Drills;
using MDS.Shared;
using MDS.Shared.Game;

namespace MDS.Features.Programs
{
    // What a running program uses: the drills, the fight watcher, and the console for its broadcasts.
    public sealed class ProgramKit
    {
        public DrillCatalog Drills { get; set; }

        public FightWatcher Watcher { get; set; }

        public GameConsole Console { get; set; }

        public Replies Replies { get; set; }

        public IClock Clock { get; set; }
    }
}
