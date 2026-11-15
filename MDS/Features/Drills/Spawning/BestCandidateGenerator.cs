using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills.Spawning
{
    // Mitchell's best candidate: each new spot is the one of several random candidates farthest from those already placed.
    // That covers the area evenly whatever the count, so the gap grows by itself with fewer players and spacing is only a floor.
    public sealed class BestCandidateGenerator : ISpotGenerator
    {
        // Mitchell's rule: the candidates grow with the spots already placed, so a filling area still finds its last gaps.
        private const int CandidatesPerPlacedSpot = 10;
        private const int FewestCandidates = 30;
        private const int MostCandidates = 1000;

        private readonly System.Random _random;

        public BestCandidateGenerator(System.Random random)
        {
            _random = random;
        }

        public List<Vector2> Generate(SpotArea area, float spacing, int count)
        {
            var spots = new List<Vector2>(count);
            while (spots.Count < count)
            {
                Vector2 best = BestCandidate(area, spots, out float clearance);
                if (clearance < spacing) break;

                spots.Add(best);
            }
            return spots;
        }

        private static float DistanceToNearest(Vector2 spot, List<Vector2> spots)
        {
            float nearest = float.MaxValue;
            foreach (Vector2 other in spots) nearest = Mathf.Min(nearest, Vector2.Distance(spot, other));
            return nearest;
        }

        // The clearance is the best candidate's distance to its nearest spot, or below any spacing when no candidate landed in the area.
        private Vector2 BestCandidate(SpotArea area, List<Vector2> spots, out float clearance)
        {
            Vector2 best = default;
            clearance = float.NegativeInfinity;
            int candidates = Mathf.Clamp(spots.Count * CandidatesPerPlacedSpot, FewestCandidates, MostCandidates);
            for (int i = 0; i < candidates; i++)
            {
                if (!area.TryDraw(_random, out Vector2 candidate)) continue;

                float distance = DistanceToNearest(candidate, spots);
                if (distance <= clearance) continue;

                best = candidate;
                clearance = distance;
            }
            return best;
        }
    }
}
