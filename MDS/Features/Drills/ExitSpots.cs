using System;
using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Drills
{
    // Where players cleared from a drill land around a spawn: at random, a body apart from each other and from anyone already standing there.
    // The API gives a spawn only its center, not its box or nodes, so the spots scatter over a disc that grows with the number sent.
    public sealed class ExitSpots
    {
        // About two players' width.
        private const float Gap = 1.2f;
        private const float BaseRadius = 2.5f;
        private const float RadiusPerPlayer = 0.4f;
        private const int TriesPerSpot = 30;
        private const float FullTurn = 2f * Mathf.PI;

        private readonly System.Random _random;

        public ExitSpots(System.Random random)
        {
            _random = random;
        }

        // A disc too crowded for the next spot grows by a gap, so the loop always ends.
        public List<Vector2> Around(ExitArea area, int count)
        {
            var spots = new List<Vector2>(count);
            float radius = BaseRadius + RadiusPerPlayer * count;
            while (spots.Count < count)
            {
                if (!TryPick(area, radius, out Vector2 spot))
                {
                    radius += Gap;
                    continue;
                }
                spots.Add(spot);
                area.Taken.Add(spot);
            }
            return spots;
        }

        private bool TryPick(ExitArea area, float radius, out Vector2 spot)
        {
            for (int attempt = 0; attempt < TriesPerSpot; attempt++)
            {
                spot = area.Spawn + InDisc(radius);
                if (!area.Zone.Contains(spot) && IsClear(spot, area.Taken)) return true;
            }
            spot = default;
            return false;
        }

        // The square root spreads the spots evenly over the disc instead of bunching them at its center.
        private Vector2 InDisc(float radius)
        {
            float angle = (float)_random.NextDouble() * FullTurn;
            float distance = radius * (float)Math.Sqrt(_random.NextDouble());
            return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
        }

        private static bool IsClear(Vector2 spot, List<Vector2> taken)
        {
            foreach (Vector2 other in taken)
            {
                if ((other - spot).sqrMagnitude < Gap * Gap) return false;
            }
            return true;
        }
    }
}
