using System.Collections.Generic;

namespace MDS.Features.Drills.Custom
{
    // Where everyone not on a static spot stands: an open melee strategy over the arena, or a line per team.
    public interface IFormation
    {
        // For the drill's summary, such as "spread" or "lines".
        string Name { get; }

        // As written, for rc customdrill show.
        IReadOnlyList<string> Lines { get; }

        FormationPlan Plan(FormationRequest request);
    }
}
