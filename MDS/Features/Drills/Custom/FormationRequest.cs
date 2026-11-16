using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Settings;
using MDS.Features.Drills.Spawning;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    public sealed class FormationRequest
    {
        public Arena Arena { get; set; }

        // In degrees, already drawn when the drill's turn is random.
        public float Turn { get; set; }

        // Everyone not on a static spot.
        public Lineup Rest { get; set; }

        // Each team's static spots, turned, which a line without a facing faces.
        public Dictionary<TeamSide, List<Vector2>> Spots { get; set; }

        public DrillSettings Settings { get; set; }

        public System.Random Random { get; set; }

        public OpenMeleePlanner OpenMelee { get; set; }
    }

    // The formation's players paired with their spots.
    public sealed class FormationPlan
    {
        public SpawnPlan Plan { get; set; } = new SpawnPlan();

        // Where an open melee formation draws from, for the preview's border.
        public Rect Area { get; set; }

        // Lines shrink with the static spots to fit the arena; an open melee formation fills the arena already.
        public bool ShrinksWithSpots { get; set; }

        // The least spacing on any line, which shrinking must keep above the floor.
        public float SmallestSpacing { get; set; } = float.MaxValue;

        // A random draw, which the drill makes again when it runs.
        public bool IsSample { get; set; }

        public string Error { get; set; } = string.Empty;
    }
}
