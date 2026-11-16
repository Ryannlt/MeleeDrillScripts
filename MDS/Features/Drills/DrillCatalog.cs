using System;
using System.Collections.Generic;

namespace MDS.Features.Drills
{
    // What other features use to run drills: each drill by name, the runner that starts them, and the kit they share.
    public sealed class DrillCatalog
    {
        public DrillCatalog(IReadOnlyList<IDrill> drills, DrillRunner runner, DrillKit kit)
        {
            Drills = drills;
            Runner = runner;
            Kit = kit;
        }

        public IReadOnlyList<IDrill> Drills { get; }

        public DrillRunner Runner { get; }

        public DrillKit Kit { get; }

        public bool TryFind(string name, out IDrill drill)
        {
            drill = null;
            foreach (IDrill candidate in Drills)
            {
                if (candidate.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) drill = candidate;
            }
            return drill != null;
        }
    }
}
