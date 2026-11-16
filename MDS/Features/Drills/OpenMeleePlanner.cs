using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Spawning;
using MDS.Shared;

namespace MDS.Features.Drills
{
    // Where an open melee puts its players, shared by rc openmelee and a custom drill's open melee formation.
    public sealed class OpenMeleePlanner
    {
        private readonly DrillKit _kit;
        private readonly SpawnPatterns _patterns;

        public OpenMeleePlanner(DrillKit kit)
        {
            _kit = kit;
            _patterns = new SpawnPatterns(kit.Random);
        }

        public static string NameOf(SpawnStrategy strategy) => strategy.ToString().ToLowerInvariant();

        // The arena comes in, so a custom drill can pass the one it runs in.
        public SpawnRequest RequestFor(DrillOptions options, Arena arena)
        {
            float offset = options.GetNumber(DrillOption.Offset, _kit.Settings.OpenMeleeOffset.Value);
            return new SpawnRequest
            {
                Strategy = _patterns.Resolve(options.GetSpawnStrategy(_kit.Settings.OpenMeleeStrategy.Value)),
                Area = OpenMeleeDrill.Area(arena, offset),
                Offset = offset,
                Spacing = options.GetNumber(DrillOption.Spacing, _kit.Settings.OpenMeleeSpacing.Value),
                Degrees = options.GetFacing(_kit.Settings.Orientation.Value).Resolve(_kit.Random),
            };
        }

        // The size checks come before any spot is drawn: a tiny spacing over a big arena needs a grid too large to build.
        public string Refusal(SpawnRequest request, Lineup field, out SpawnPlan plan)
        {
            plan = new SpawnPlan();
            int players = field.Attackers.Count + field.Defenders.Count;
            if (players == 0) return "Nobody is on the field for an open melee.";
            if (request.Spacing < DrillKit.MinimumSpacing) return $"Spacing must be at least {Numbers.Format(DrillKit.MinimumSpacing)} m.";
            ISpawnPattern pattern = _patterns.For(request.Strategy);
            if (!pattern.CanCover(request)) return $"The arena is too big for {NameOf(request.Strategy)} at that spacing: raise spacing, or use spread or groups.";

            if (request.Area.width > 0f) plan = FullestPlan(field, request);
            if (plan.Spots.Count >= players) return string.Empty;

            return $"The arena has room for {plan.Spots.Count} of {players} players {pattern.DescribeSpacing(request)} and {Numbers.Format(request.Offset)} m in from the edges.";
        }

        public SpawnPlan FullestPlan(Lineup field, SpawnRequest request)
        {
            ISpawnPattern pattern = _patterns.For(request.Strategy);
            return SpawnPlan.Fullest(() => pattern.Plan(field, request));
        }
    }
}
