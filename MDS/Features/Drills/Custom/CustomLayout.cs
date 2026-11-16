using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Settings;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // Where everyone stands for one run: the static spots turned around the arena's center, then the formation for everyone else.
    // When a static spot or a line would stand outside the arena, they shrink toward the center together by the largest share that fits.
    // How close the drill puts its own players is the drill maker's choice, so only shrinking is held to the spacing floor.
    public sealed class CustomLayout
    {
        private const float Half = 0.5f;
        private const float Percent = 100f;

        // Room for float rounding when a line ends up exactly at the floor.
        private const float Slack = 0.001f;

        private readonly DrillSettings _settings;
        private readonly System.Random _random;
        private readonly OpenMeleePlanner _openMelee;

        public CustomLayout(DrillKit kit)
        {
            _settings = kit.Settings;
            _random = kit.Random;
            _openMelee = new OpenMeleePlanner(kit);
        }

        public static Vector2 Turned(Vector2 offset, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
        }

        // The rest are everyone the formation places.
        public CustomPlacement Place(CustomRun run, Lineup rest)
        {
            var placement = new CustomPlacement();
            foreach (StaticSpot spot in run.Drill.Spots) placement.SpotsOf(run.SideOf(spot.Side)).Add(run.Arena.Center + Turned(spot.Offset, run.Degrees));
            if (run.Drill.Formation != null) placement.Formation = run.Drill.Formation.Plan(FormationRequestFor(run, rest, placement));

            placement.Error = placement.Formation.Error;
            if (placement.Fits) Fit(placement, run);
            return placement;
        }

        private static void Fit(CustomPlacement placement, CustomRun run)
        {
            float share = ShareThatFits(placement.Shrinking(), run.Arena);
            if (share >= 1f) return;

            if (placement.Formation.SmallestSpacing * share + Slack < DrillKit.MinimumSpacing)
            {
                placement.Error = $"Arena {run.Arena.Number} is too small for {run.Drill.Name}'s lines, even shrunk to {Numbers.Format(DrillKit.MinimumSpacing)} m spacing.";
                return;
            }

            placement.ScaleToward(run.Arena.Center, share);
            placement.Note = FitNote.Of(run.Arena.Number, new[] { $"{Mathf.RoundToInt(share * Percent)}% of its size" });
        }

        // Rounded down to the hundredth, so the share the reply names still fits.
        private static float ShareThatFits(List<Vector2> spots, Arena arena)
        {
            Vector2 half = (arena.Max - arena.Min) * Half;
            float share = 1f;
            foreach (Vector2 spot in spots)
            {
                Vector2 from = spot - arena.Center;
                share = Mathf.Min(share, Mathf.Abs(from.x) > half.x ? half.x / Mathf.Abs(from.x) : 1f, Mathf.Abs(from.y) > half.y ? half.y / Mathf.Abs(from.y) : 1f);
            }
            return share < 1f ? Numbers.RoundDown(share) : share;
        }

        private FormationRequest FormationRequestFor(CustomRun run, Lineup rest, CustomPlacement placement)
        {
            return new FormationRequest
            {
                Arena = run.Arena,
                Turn = run.Degrees,
                Rest = rest,
                Spots = new Dictionary<TeamSide, List<Vector2>> { [TeamSide.Attacker] = new(placement.Attackers), [TeamSide.Defender] = new(placement.Defenders) },
                Settings = _settings,
                Random = _random,
                OpenMelee = _openMelee,
                Mirrored = run.Mirrored,
            };
        }
    }
}
