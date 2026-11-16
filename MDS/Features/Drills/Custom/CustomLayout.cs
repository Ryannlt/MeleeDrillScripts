using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Spawning;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // Where each group stands for one run: its offset turned around the arena's center, then a line across its facing or a scattered pocket.
    // When anyone would stand outside the arena, the whole layout shrinks toward the center by the largest share that fits.
    public sealed class CustomLayout
    {
        private const float Half = 0.5f;
        private const float Percent = 100f;

        // Closer than this, a group has no direction to the groups it faces.
        private const float SameSpot = 0.01f;

        // Room for float rounding when two players stand exactly the smallest spacing apart.
        private const float Slack = 0.001f;

        // A random pocket holds about one player per two spacings squared, so the square starts with that much room plus a spacing, and grows when it comes up short.
        private const float PocketDensity = 0.5f;
        private const float PocketGrowth = 1.5f;
        private const int PocketAttempts = 3;

        private readonly System.Random _random;
        private readonly ISpotGenerator _organic;
        private readonly ISpotGenerator _even;

        public CustomLayout(System.Random random)
        {
            _random = random;
            _organic = new OrganicGenerator(random);
            _even = new BestCandidateGenerator(random);
        }

        // The turn is in degrees, already drawn when the drill's turn is random.
        // The drill's own spacing is checked before the arena's size, so crowded groups are never blamed on the arena.
        public CustomPlacement Place(CustomDrill drill, Arena arena, float turn)
        {
            CustomPlacement placement = Laid(drill, arena, turn);
            placement.Error = ShortGroup(placement, drill);
            if (!placement.Fits) return placement;

            List<Vector2> spots = placement.AllSpots();
            float closest = ClosestTwo(spots);
            float share = ShareThatFits(spots, arena);
            if (closest + Slack < DrillKit.MinimumSpacing) placement.Error = $"{drill.Name} stands two players under {Numbers.Format(DrillKit.MinimumSpacing)} m apart: move its groups apart.";
            else if (closest * share + Slack < DrillKit.MinimumSpacing) placement.Error = $"Arena {arena.Number} is too small for {drill.Name}, even shrunk to {Numbers.Format(DrillKit.MinimumSpacing)} m spacing.";
            else if (share < 1f) Shrink(placement, arena, share);
            return placement;
        }

        private static bool IsDrawn(DrillGroup group) => group.Shape == GroupShape.Scatter || (group.Facing.HasValue && group.Facing.Value.IsRandom);

        private static Vector2 Turned(Vector2 offset, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(offset.x * cos - offset.y * sin, offset.x * sin + offset.y * cos);
        }

        private static string ShortGroup(CustomPlacement placement, CustomDrill drill)
        {
            for (int i = 0; i < drill.Groups.Count; i++)
            {
                if (placement.Groups[i].Spots.Count < drill.Groups[i].Count) return $"{drill.Name} has no room for its group '{drill.Groups[i].Text}' at that spacing.";
            }
            return string.Empty;
        }

        // Across groups as well as within them, so no two players ever stand on top of each other.
        private static float ClosestTwo(List<Vector2> spots)
        {
            float closest = float.MaxValue;
            for (int i = 0; i < spots.Count; i++)
            {
                for (int j = i + 1; j < spots.Count; j++) closest = Mathf.Min(closest, Vector2.Distance(spots[i], spots[j]));
            }
            return closest;
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

        private static void Shrink(CustomPlacement placement, Arena arena, float share)
        {
            placement.ScaleToward(arena.Center, share);
            placement.Note = FitNote.Of(arena.Number, new[] { $"{Mathf.RoundToInt(share * Percent)}% of its size" });
        }

        private CustomPlacement Laid(CustomDrill drill, Arena arena, float turn)
        {
            var sketch = new Sketch(drill, arena, turn);
            var placement = new CustomPlacement { IsSample = drill.Groups.Exists(IsDrawn) };
            for (int i = 0; i < drill.Groups.Count; i++)
            {
                DrillGroup group = drill.Groups[i];
                placement.Groups.Add(new PlacedGroup(group.Side, SpotsOf(group, sketch.Centers[i], DegreesOf(sketch, i))));
            }
            return placement;
        }

        // A facing written for the group turns with the drill; without one the group faces the middle of the other side's groups.
        private float DegreesOf(Sketch sketch, int index)
        {
            Facing? written = sketch.Drill.Groups[index].Facing;
            if (written.HasValue) return written.Value.IsRandom ? written.Value.Resolve(_random) : written.Value.Degrees + sketch.Turn;

            Vector2 toward = sketch.MiddleOfOtherSide(index) - sketch.Centers[index];
            return toward.magnitude < SameSpot ? sketch.Turn : Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg;
        }

        private List<Vector2> SpotsOf(DrillGroup group, Vector2 center, float degrees)
        {
            if (group.Shape == GroupShape.Scatter) return ScatterSpots(group, center);

            return LinePlacement.AttackerSpots(new LineShape { Center = center, Spacing = group.Spacing, Degrees = degrees }, group.Count);
        }

        private List<Vector2> ScatterSpots(DrillGroup group, Vector2 center)
        {
            float side = group.Spacing * (Mathf.Sqrt(group.Count / PocketDensity) + 1f);
            var spots = new List<Vector2>();
            for (int attempt = 0; attempt < PocketAttempts && spots.Count < group.Count; attempt++, side *= PocketGrowth) spots = PocketSpots(group, center, side);
            return spots;
        }

        // Organic growth keeps the group one pocket, and the even spread is the fallback.
        private List<Vector2> PocketSpots(DrillGroup group, Vector2 center, float side)
        {
            var box = SpotArea.Whole(new Rect(center - Vector2.one * side * Half, Vector2.one * side));
            List<Vector2> spots = _organic.Generate(box, group.Spacing, group.Count);
            return spots.Count >= group.Count ? spots : _even.Generate(box, group.Spacing, group.Count);
        }

        // The group centers for one run, turned around the arena's center.
        private sealed class Sketch
        {
            public Sketch(CustomDrill drill, Arena arena, float turn)
            {
                Drill = drill;
                Turn = turn;
                ArenaCenter = arena.Center;
                Centers = drill.Groups.ConvertAll(group => arena.Center + Turned(group.Offset, turn));
            }

            public CustomDrill Drill { get; }

            public float Turn { get; }

            public Vector2 ArenaCenter { get; }

            public List<Vector2> Centers { get; }

            // The arena's center when the other side has no groups.
            public Vector2 MiddleOfOtherSide(int index)
            {
                TeamSide side = Drill.Groups[index].Side;
                Vector2 sum = Vector2.zero;
                int count = 0;
                for (int i = 0; i < Centers.Count; i++)
                {
                    if (Drill.Groups[i].Side == side) continue;

                    sum += Centers[i];
                    count++;
                }
                return count > 0 ? sum / count : ArenaCenter;
            }
        }
    }
}
