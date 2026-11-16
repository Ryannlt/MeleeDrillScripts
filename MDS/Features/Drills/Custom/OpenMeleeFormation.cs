using System.Collections.Generic;
using MDS.Features.Drills.Spawning;

namespace MDS.Features.Drills.Custom
{
    // Everyone not on a static spot spread over the arena by an open melee strategy, written the way rc openmelee takes it: spread,spacing=2,offset=3.
    public sealed class OpenMeleeFormation : IFormation
    {
        private readonly DrillOptions _options;

        public OpenMeleeFormation(DrillOptions options, string text)
        {
            _options = options;
            Lines = new[] { text };
        }

        public string Name => OpenMeleePlanner.NameOf(_options.GetSpawnStrategy(SpawnStrategy.Random));

        public IReadOnlyList<string> Lines { get; }

        public FormationPlan Plan(FormationRequest request)
        {
            SpawnRequest spawn = request.OpenMelee.RequestFor(_options, request.Arena);
            spawn.Degrees += request.Turn;
            var plan = new FormationPlan { Area = spawn.Area, IsSample = true };
            if (request.Rest.Attackers.Count + request.Rest.Defenders.Count == 0) return plan;

            plan.Error = request.OpenMelee.Refusal(spawn, request.Rest, out SpawnPlan spots);
            plan.Plan = spots;
            return plan;
        }
    }
}
