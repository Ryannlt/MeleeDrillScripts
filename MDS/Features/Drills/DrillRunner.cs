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
        public bool TryStart(IDrill drill, DrillOptions options, out string error)
        {
            if (!drill.TryStart(options, out DrillRun run, out error)) return false;

            if (drill.ClearsLeftovers) _clearing.Clear(run);
            return true;
        }
    }
}
