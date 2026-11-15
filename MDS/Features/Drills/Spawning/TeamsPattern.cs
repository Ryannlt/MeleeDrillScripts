using MDS.Features.Drills.Picking;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Each team scatters over its own half, split across the facing, attackers on the side the facing points away from.
    // Each half stops half the spacing short of the middle line, so spacing stays the least distance between any two players.
    public sealed class TeamsPattern : ISpawnPattern
    {
        private const float Half = 0.5f;

        private readonly ISpotGenerator _generator;

        public TeamsPattern(ISpotGenerator generator)
        {
            _generator = generator;
        }

        public SpawnPlan Plan(Lineup field, SpawnRequest request)
        {
            float radians = request.Degrees * Mathf.Deg2Rad;
            var towardDefenders = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            float gap = request.Spacing * Half;

            var plan = new SpawnPlan();
            plan.Add(field.Attackers, _generator.Generate(SpotArea.Half(request.Area, -towardDefenders, gap), request.Spacing, field.Attackers.Count));
            plan.Add(field.Defenders, _generator.Generate(SpotArea.Half(request.Area, towardDefenders, gap), request.Spacing, field.Defenders.Count));
            return plan;
        }
    }
}
