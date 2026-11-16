using System.Collections.Generic;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // One team's line as written: line,attacker,(0,-8),spacing=2,facing=northsouth.
    public sealed class FormationLine
    {
        public FormationLine(DrillOptions options, string text)
        {
            Options = options;
            Text = text;
        }

        public TeamSide Side => Options.GetSide();

        public DrillOptions Options { get; }

        public string Text { get; }
    }

    // A line for each team written, each taking every player of its team not on a static spot.
    // A line without a facing faces the middle of the other team's static spots and line, or the arena's center when the other team has neither.
    public sealed class LineFormation : IFormation
    {
        // Closer than this, a line has no direction to what it faces.
        private const float SameSpot = 0.01f;

        private readonly Dictionary<TeamSide, FormationLine> _lines = new();

        public LineFormation(FormationLine line)
        {
            Set(line);
        }

        public string Name => "lines";

        public IReadOnlyList<string> Lines => OrderedLines().ConvertAll(line => line.Text);

        // A second line for the same team replaces the first.
        public void Set(FormationLine line) => _lines[line.Side] = line;

        public FormationPlan Plan(FormationRequest request)
        {
            var plan = new FormationPlan { ShrinksWithSpots = true };
            foreach (FormationLine line in OrderedLines())
            {
                IReadOnlyList<IPlayer> players = request.SideOf(line.Side) == TeamSide.Attacker ? request.Rest.Attackers : request.Rest.Defenders;
                LineShape shape = ShapeOf(line, request);
                plan.Plan.Add(players, LinePlacement.AttackerSpots(shape, players.Count));
                if (players.Count > 1) plan.SmallestSpacing = Mathf.Min(plan.SmallestSpacing, shape.Spacing);
                plan.IsSample |= shape.FacingIsRandom;
            }
            return plan;
        }

        private static Vector2 CenterOf(FormationLine line, FormationRequest request) => request.Arena.Center + CustomLayout.Turned(line.Options.GetPosition(), request.Turn);

        private List<FormationLine> OrderedLines()
        {
            var lines = new List<FormationLine>();
            foreach (TeamSide side in new[] { TeamSide.Attacker, TeamSide.Defender })
            {
                if (_lines.TryGetValue(side, out FormationLine line)) lines.Add(line);
            }
            return lines;
        }

        private LineShape ShapeOf(FormationLine line, FormationRequest request)
        {
            Vector2 center = CenterOf(line, request);
            Facing facing = line.Options.GetFacing(Facing.Random);
            return new LineShape
            {
                Center = center,
                Spacing = line.Options.GetNumber(DrillOption.Spacing, request.Settings.GroupfightSpacing.Value),
                Degrees = line.Options.Has(DrillOption.Facing) ? WrittenDegrees(facing, request) : DegreesToward(MiddleOfOther(line.Side, request), center, request.Turn),
                FacingIsRandom = line.Options.Has(DrillOption.Facing) && facing.IsRandom,
            };
        }

        // A written facing turns with the drill, and a random one is drawn each run.
        private static float WrittenDegrees(Facing facing, FormationRequest request) => facing.IsRandom ? facing.Resolve(request.Random) : facing.Degrees + request.Turn;

        private static float DegreesToward(Vector2 target, Vector2 center, float turn)
        {
            Vector2 toward = target - center;
            return toward.magnitude < SameSpot ? turn : Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg;
        }

        // Mirroring flips every spot and line alike, so the other written line still stands for the other team.
        private Vector2 MiddleOfOther(TeamSide written, FormationRequest request)
        {
            var points = new List<Vector2>(request.Spots[Teams.Other(request.SideOf(written))]);
            if (_lines.TryGetValue(Teams.Other(written), out FormationLine line)) points.Add(CenterOf(line, request));
            if (points.Count == 0) return request.Arena.Center;

            Vector2 sum = Vector2.zero;
            foreach (Vector2 point in points) sum += point;
            return sum / points.Count;
        }
    }
}
