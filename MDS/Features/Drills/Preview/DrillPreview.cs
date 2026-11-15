using System.Collections.Generic;
using System.Linq;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Spawning;
using UnityEngine;

namespace MDS.Features.Drills.Preview
{
    public sealed class DrillPreview
    {
        public List<Vector2> Attackers { get; } = new();

        public List<Vector2> Defenders { get; } = new();

        // Where open melee draws its spots from, and empty for drills that place lines.
        public Rect Area { get; set; }

        // A random draw, which the drill makes again when it runs.
        public bool IsSample { get; set; }

        // Each placed player's spot goes on their team's side.
        public void AddSpots(Lineup field, SpawnPlan plan)
        {
            for (int i = 0; i < plan.Players.Count; i++) (field.Attackers.Contains(plan.Players[i]) ? Attackers : Defenders).Add(plan.Spots[i]);
        }

        // What the reply adds after the drill's name, such as the spawn strategy drawn.
        public string Detail { get; set; } = string.Empty;

        // What the drill changed to fit its arena.
        public string Note { get; set; } = string.Empty;
    }
}
