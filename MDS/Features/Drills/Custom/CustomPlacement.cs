using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // One run of a custom drill, carried from the command to the move, which may come after a countdown.
    public sealed class CustomRun
    {
        public CustomDrill Drill { get; set; }

        public Arena Arena { get; set; }

        public Facing Turn { get; set; }

        // The turn in degrees, drawn once so the check, the preview and the move agree.
        public float Degrees { get; set; }

        // The players picked for the static spots.
        public Lineup Picked { get; set; }

        public DrillRun Run { get; set; }
    }

    // Where everyone stands for one run: each team's static spots in the drill's order, and the formation's players with their spots.
    public sealed class CustomPlacement
    {
        public List<Vector2> Attackers { get; } = new();

        public List<Vector2> Defenders { get; } = new();

        public FormationPlan Formation { get; set; } = new FormationPlan();

        public string Note { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;

        public bool Fits => Error.Length == 0;

        public List<Vector2> SpotsOf(TeamSide side) => side == TeamSide.Attacker ? Attackers : Defenders;

        public List<Vector2> AllSpots()
        {
            var spots = new List<Vector2>(Attackers);
            spots.AddRange(Defenders);
            spots.AddRange(Formation.Plan.Spots);
            return spots;
        }

        // The static spots, and the formation's when it shrinks with them.
        public List<Vector2> Shrinking()
        {
            var spots = new List<Vector2>(Attackers);
            spots.AddRange(Defenders);
            if (Formation.ShrinksWithSpots) spots.AddRange(Formation.Plan.Spots);
            return spots;
        }

        // Every shrinking spot moves toward the center by the same share, so the layout keeps its shape.
        public void ScaleToward(Vector2 center, float share)
        {
            Scale(Attackers, center, share);
            Scale(Defenders, center, share);
            if (Formation.ShrinksWithSpots) Scale(Formation.Plan.Spots, center, share);
        }

        private static void Scale(List<Vector2> spots, Vector2 center, float share)
        {
            for (int i = 0; i < spots.Count; i++) spots[i] = center + (spots[i] - center) * share;
        }
    }
}
