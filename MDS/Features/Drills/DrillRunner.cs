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
        // Null when the drill refused.
        public DrillRun Start(IDrill drill, DrillOptions options, Action<string> tell)
        {
            if (!drill.TryStart(options, out DrillRun run, out string error))
            {
                tell(error);
                return null;
            }

            if (drill.ClearsLeftovers) ClearWhenPlaced(run);
            run.Noticed += tell;
            if (run.Note.Length > 0) tell(run.Note);
            return run;
        }

        // A drill that counts down places its players later, and the clearing waits for them.
        private void ClearWhenPlaced(DrillRun run)
        {
            if (run.PlacedAnyone) _clearing.Clear(run);
            else run.PlayersPlaced += _clearing.Clear;
        }
    }
}
