using System;
using MDS.Features.Drills.Picking;

namespace MDS.Features.Programs
{
    // How a step deals the free players over a drill line's arenas: whether a fight takes the drill as written or mirrored.
    public interface IArenaStrategy
    {
        bool ShouldMirror(Headcount needs, Lineup free);
    }

    // Every fight as written, so a 2v1 always gives the attackers the two.
    public sealed class FillArenas : IArenaStrategy
    {
        public bool ShouldMirror(Headcount needs, Lineup free) => false;
    }

    // Each fight's sides chosen to play the most people: the mirror when only it fits, or when it leaves the free teams more level.
    public sealed class BalanceArenas : IArenaStrategy
    {
        public bool ShouldMirror(Headcount needs, Lineup free)
        {
            if (needs.IsEveryone || needs.Attackers == needs.Defenders) return false;

            Headcount mirror = needs.Mirrored;
            bool written = Fits(needs, free);
            bool mirrored = Fits(mirror, free);
            if (written != mirrored) return mirrored;
            return mirrored && Gap(mirror, free) < Gap(needs, free);
        }

        private static bool Fits(Headcount count, Lineup free) => count.Attackers <= free.Attackers.Count && count.Defenders <= free.Defenders.Count;

        // How uneven the free teams would be after this fight.
        private static int Gap(Headcount count, Lineup free) => Math.Abs((free.Attackers.Count - count.Attackers) - (free.Defenders.Count - count.Defenders));
    }
}
