using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Spawning;
using UnityEngine;

namespace MDS.Features.Drills.Popcorn
{
    public sealed class PopcornRequest
    {
        // Resolved: never Random.
        public FightLayout Layout { get; set; }

        public FightSize Smallest { get; set; }

        public FightSize Largest { get; set; }

        public Rect Area { get; set; }

        // The least distance between players of different fights: Tight packs the fights as close as it allows, the others spread them farther.
        public float Distance { get; set; }

        // Between a fight's two lines.
        public float LineDistance { get; set; }

        // Between neighbors on a line.
        public float Spacing { get; set; }
    }

    // Each fight is two facing lines turned a random way around its own center.
    // Centers sit two fight reaches plus the distance apart and a reach in from the arena's edges, so different fights stay the distance apart and every fight stays in the arena.
    public sealed class PopcornLayout
    {
        private const float FullTurn = 360f;
        private const float Half = 0.5f;

        private static readonly FightLayout[] Drawable = { FightLayout.Scatter, FightLayout.Spread, FightLayout.Tight };

        private readonly System.Random _random;
        private readonly Dictionary<FightLayout, ISpotGenerator> _centers;

        public PopcornLayout(System.Random random)
        {
            _random = random;
            _centers = new Dictionary<FightLayout, ISpotGenerator>
            {
                [FightLayout.Scatter] = new OrganicGenerator(random),
                [FightLayout.Spread] = new BestCandidateGenerator(random),
                [FightLayout.Tight] = new PackedGenerator(random),
            };
        }

        public FightLayout Resolve(FightLayout layout) => layout == FightLayout.Random ? Drawable[_random.Next(Drawable.Length)] : layout;

        // Fights without room are left unplaced in the plan.
        public SpawnPlan Place(List<Lineup> fights, PopcornRequest request)
        {
            float reach = ReachOf(fights, request);
            List<Vector2> centers = FitsAFight(request.Area, reach)
                ? _centers[request.Layout].Generate(SpotArea.Whole(SpotArea.Shrunk(request.Area, reach)), request.Distance + reach + reach, fights.Count)
                : new List<Vector2>();
            var plan = new SpawnPlan();
            for (int i = 0; i < fights.Count; i++)
            {
                if (i < centers.Count) AddFight(plan, fights[i], ShapeAt(centers[i], request));
                else plan.Add(fights[i].Everyone(), new List<Vector2>());
            }
            return plan;
        }

        private static bool FitsAFight(Rect area, float reach) => area.width >= reach + reach && area.height >= reach + reach;

        // The half diagonal of the biggest fight's lines, so a fight turned any way fits within it.
        private static float ReachOf(List<Lineup> fights, PopcornRequest request)
        {
            int longest = 1;
            foreach (Lineup fight in fights) longest = Mathf.Max(longest, Mathf.Max(fight.Attackers.Count, fight.Defenders.Count));
            return new Vector2((longest - 1) * request.Spacing * Half, request.LineDistance * Half).magnitude;
        }

        private static void AddFight(SpawnPlan plan, Lineup fight, LineShape shape)
        {
            plan.Add(fight.Attackers, LinePlacement.AttackerSpots(shape, fight.Attackers.Count));
            plan.Add(fight.Defenders, LinePlacement.DefenderSpots(shape, fight.Defenders.Count));
        }

        private LineShape ShapeAt(Vector2 center, PopcornRequest request)
        {
            return new LineShape
            {
                Center = center,
                Distance = request.LineDistance,
                Spacing = request.Spacing,
                Degrees = (float)(_random.NextDouble() * FullTurn),
            };
        }
    }
}
