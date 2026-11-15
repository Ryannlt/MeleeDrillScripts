using System;

namespace MDS.Features.Drills
{
    public sealed class DrillRunner
    {
        private readonly LeftoverClearing _clearing;

        public DrillRunner(LeftoverClearing clearing)
        {
            _clearing = clearing;
        }

        // The lineup is placed by then, but its players are never cleared.
        // Every teleport runs in the same server frame, so nobody plays a moment with the leftovers.
        // Whoever started the drill is told why it was refused, what it changed to fit, and what goes wrong after the start.
        public bool TryStart(IDrill drill, DrillOptions options, Action<string> tell)
        {
            if (!drill.TryStart(options, out DrillRun run, out string error))
            {
                tell(error);
                return false;
            }

            if (drill.ClearsLeftovers) _clearing.Clear(run);
            run.Noticed += tell;
            if (run.Note.Length > 0) tell(run.Note);
            return true;
        }
    }
}
