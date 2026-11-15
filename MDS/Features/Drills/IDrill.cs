namespace MDS.Features.Drills
{
    public interface IDrill
    {
        string Name { get; }

        DrillSyntax Syntax { get; }

        // A drill that picks only some players also sends anyone else standing where it places them back to a spawn.
        bool ClearsLeftovers { get; }

        // What can be known before the drill runs: an arena, teams, a strategy it can use.
        bool Validate(DrillOptions options, out string error);

        // The pick happens here, so too few players still refuses after Validate passed.
        bool TryStart(DrillOptions options, out DrillRun run, out string error);
    }
}
