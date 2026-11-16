using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;

namespace MDS.Features.Drills
{
    public interface IDrill
    {
        string Name { get; }

        DrillSyntax Syntax { get; }

        // A drill that picks only some players also sends anyone else standing where it places them back to a spawn.
        bool ClearsLeftovers { get; }

        // A drill with a set number of players can run in several arenas at once, one fight each; one that takes everyone can't.
        bool RunsInParallel(DrillOptions options);

        // The players it takes as written, before any mirror: Headcount.Everyone for a drill that takes the whole field.
        Headcount Needs(DrillOptions options);

        // What can be known before the drill runs: an arena, teams, a strategy it can use.
        bool Validate(DrillOptions options, out string error);

        // The pick happens here, so too few players still refuses after Validate passed.
        bool TryStart(DrillOptions options, out DrillRun run, out string error);

        // Where the drill would put players now, without moving or picking anyone.
        bool TryPreview(DrillOptions options, out DrillPreview preview, out string error);
    }
}
