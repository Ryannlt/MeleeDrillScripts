using System.Collections.Generic;

namespace MDS.Features.Drills.Picking
{
    // The last lineup overall for rc, and the last in each arena for a program's arenas.
    public sealed class LineupMemory
    {
        private readonly Dictionary<int, Lineup> _byArena = new();

        public Lineup Last { get; private set; }

        public Lineup LastIn(int arena) => _byArena.TryGetValue(arena, out Lineup lineup) ? lineup : null;

        public void Remember(int arena, Lineup lineup)
        {
            Last = lineup;
            _byArena[arena] = lineup;
        }

        public void Clear()
        {
            Last = null;
            _byArena.Clear();
        }
    }
}
