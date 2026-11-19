using MDS.Features.Arenas;
using MDS.Features.Drills.Custom;
using MDS.Features.Programs;

namespace MDS.Features.Panel
{
    public sealed class PanelLists
    {
        public PanelLists(ArenaStore arenas, CustomDrillBook drills, ProgramsFeature programs)
        {
            Arenas = arenas;
            Drills = drills;
            Programs = programs;
        }

        public ArenaStore Arenas { get; }
        public CustomDrillBook Drills { get; }
        public ProgramsFeature Programs { get; }
    }
}
