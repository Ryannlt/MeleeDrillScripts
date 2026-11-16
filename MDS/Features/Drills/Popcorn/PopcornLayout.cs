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

        // Who is free to be dealt into a fight.
        public PickScope Scope { get; set; }

        public PopcornRequest WithDistance(float distance)
        {
            return new PopcornRequest { Layout = Layout, Smallest = Smallest, Largest = Largest, Area = Area, Distance = distance, LineDistance = LineDistance, Spacing = Spacing, Scope = Scope };
        }
    }

    // Each fight is two facing lines turned a random way around its own center, and reaches as far as the half diagonal of its lines.
    // Two centers sit both fights' reaches plus the distance apart and each a reach in from the arena's edges, so fights stay the distance apart and inside the arena.
    // The layout's generator lays out spare centers at the smallest fight's spacing, and the fights take them biggest first, so a big fight doesn't push the small ones apart.
    public sealed class PopcornLayout
    {
        private const float FullTurn = 360f;
        private const float Half = 0.5f;

        // Spares for the centers a bigger neighbor crowds out.
        private const int CentersPerFight = 6;

        // At the smallest fight's spacing a big fight blocks the whole ring of comb points around it, and the next fight lands two rings out.
        // A comb three times finer lets each fight sit as close to a big neighbor as the distance allows.
        private const int TightFineness = 3;

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
            List<SizedFight> sized = BiggestFirst(fights, request);
            var centers = new Centers(Candidates(sized, request), request);
            var plan = new SpawnPlan();
            foreach (SizedFight fight in sized)
            {
                if (centers.TryTake(fight)) AddFight(plan, fight.Lineup, ShapeAt(fight.Center, request));
                else plan.Add(fight.Lineup.Everyone(), new List<Vector2>());
            }
            return plan;
        }

        private static bool FitsAFight(Rect area, float reach) => area.width >= reach + reach && area.height >= reach + reach;

        private static List<SizedFight> BiggestFirst(List<Lineup> fights, PopcornRequest request)
        {
            List<SizedFight> sized = fights.ConvertAll(fight => new SizedFight(fight, ReachOf(fight, request)));
            sized.Sort((a, b) => b.Reach.CompareTo(a.Reach));
            return sized;
        }

        private static float ReachOf(Lineup fight, PopcornRequest request)
        {
            int longest = Mathf.Max(1, Mathf.Max(fight.Attackers.Count, fight.Defenders.Count));
            return new Vector2((longest - 1) * request.Spacing * Half, request.LineDistance * Half).magnitude;
        }

        private static void AddFight(SpawnPlan plan, Lineup fight, LineShape shape)
        {
            plan.Add(fight.Attackers, LinePlacement.AttackerSpots(shape, fight.Attackers.Count));
            plan.Add(fight.Defenders, LinePlacement.DefenderSpots(shape, fight.Defenders.Count));
        }

        private List<Vector2> Candidates(List<SizedFight> sized, PopcornRequest request)
        {
            float smallest = sized.Count > 0 ? sized[sized.Count - 1].Reach : 0f;
            if (sized.Count == 0 || !FitsAFight(request.Area, smallest)) return new List<Vector2>();

            SpotArea area = SpotArea.Whole(SpotArea.Shrunk(request.Area, smallest));
            int fineness = request.Layout == FightLayout.Tight ? TightFineness : 1;
            return _centers[request.Layout].Generate(area, (request.Distance + smallest + smallest) / fineness, sized.Count * CentersPerFight * fineness * fineness);
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

        private sealed class SizedFight
        {
            public SizedFight(Lineup lineup, float reach)
            {
                Lineup = lineup;
                Reach = reach;
            }

            public Lineup Lineup { get; }

            public float Reach { get; }

            public Vector2 Center { get; set; }
        }

        // Candidates in the generator's order, each taken by the first fight it is free for.
        private sealed class Centers
        {
            // Tight's comb puts equal fights exactly the separation apart, which float rounding can land a hair under.
            private const float Slack = 0.001f;

            private readonly List<Vector2> _candidates;
            private readonly List<SizedFight> _taken = new();
            private readonly Rect _area;
            private readonly float _distance;

            public Centers(List<Vector2> candidates, PopcornRequest request)
            {
                _candidates = candidates;
                _area = request.Area;
                _distance = request.Distance;
            }

            public bool TryTake(SizedFight fight)
            {
                int index = _candidates.FindIndex(candidate => IsFree(candidate, fight.Reach));
                if (index < 0) return false;

                fight.Center = _candidates[index];
                _candidates.RemoveAt(index);
                _taken.Add(fight);
                return true;
            }

            private bool IsFree(Vector2 center, float reach)
            {
                if (!FitsAFight(_area, reach) || !SpotArea.Whole(SpotArea.Shrunk(_area, reach)).Contains(center)) return false;

                return _taken.TrueForAll(other => Vector2.Distance(center, other.Center) + Slack >= _distance + reach + other.Reach);
            }
        }
    }
}
