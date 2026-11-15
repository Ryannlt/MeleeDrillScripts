using System.Collections.Generic;

namespace MDS.Features.Drills.Spawning
{
    public sealed class SpawnPatterns
    {
        private static readonly SpawnStrategy[] Drawable = { SpawnStrategy.Scatter, SpawnStrategy.Spread, SpawnStrategy.Groups, SpawnStrategy.Teams };

        private readonly Dictionary<SpawnStrategy, ISpawnPattern> _patterns;
        private readonly System.Random _random;

        public SpawnPatterns(System.Random random)
        {
            _random = random;
            var organic = new OrganicGenerator(random);
            var even = new BestCandidateGenerator(random);
            _patterns = new Dictionary<SpawnStrategy, ISpawnPattern>
            {
                [SpawnStrategy.Scatter] = new AreaPattern(organic, random),
                [SpawnStrategy.Spread] = new AreaPattern(even, random),
                [SpawnStrategy.Groups] = new GroupsPattern(even, organic, random),
                [SpawnStrategy.Teams] = new TeamsPattern(organic),
            };
        }

        public SpawnStrategy Resolve(SpawnStrategy strategy) => strategy == SpawnStrategy.Random ? Drawable[_random.Next(Drawable.Length)] : strategy;

        // Takes a resolved strategy: Random has no pattern of its own.
        public ISpawnPattern For(SpawnStrategy strategy) => _patterns[strategy];
    }
}
