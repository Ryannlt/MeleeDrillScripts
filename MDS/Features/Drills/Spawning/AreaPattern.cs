using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;
using MDS.Shared.Players;

namespace MDS.Features.Drills.Spawning
{
    // Everyone shuffled over the whole area, so neither team starts bunched in one part of it.
    public sealed class AreaPattern : ISpawnPattern
    {
        private readonly ISpotGenerator _generator;
        private readonly System.Random _random;

        public AreaPattern(ISpotGenerator generator, System.Random random)
        {
            _generator = generator;
            _random = random;
        }

        public bool CanCover(SpawnRequest request) => _generator.CanCover(request.Area, request.Spacing);

        public string DescribeSpacing(SpawnRequest request) => $"{Numbers.Format(request.Spacing)} m apart";

        public SpawnPlan Plan(Lineup field, SpawnRequest request)
        {
            List<IPlayer> players = PickRules.Shuffled(field.Everyone(), _random);
            var plan = new SpawnPlan();
            plan.Add(players, _generator.Generate(SpotArea.Whole(request.Area), request.Spacing, players.Count));
            return plan;
        }
    }
}
