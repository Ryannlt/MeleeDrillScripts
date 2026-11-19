using System.Collections.Generic;
using System.Linq;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;
using MDS.Features.Drills.Spawning;
using MDS.Shared;

namespace MDS.Features.Drills.Popcorn
{
    public sealed class PopcornDrill : IDrill
    {
        private const string Title = "Popcorn";

        // Just long enough that nobody is hit as the fights land.
        private const float GodModeAfterMoveSeconds = 1f;

        // Six halvings find the largest distance that fits to within a sixty-fourth of the range, under 9 cm at the default of 6 m.
        private const int SearchSteps = 6;
        private const float Half = 0.5f;

        private readonly DrillKit _kit;
        private readonly DrillCountdown _countdown;
        private readonly PopcornFights _fights;
        private readonly PopcornLayout _layout;

        public PopcornDrill(DrillKit kit, DrillCountdown countdown)
        {
            _kit = kit;
            _countdown = countdown;
            _fights = new PopcornFights(kit.Random);
            _layout = new PopcornLayout(kit.Random);
        }

        public string Name => "popcorn";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc popcorn [scatter|spread|tight|random] [min] [max] [distance] [lineDistance] [spacing] [early|late] [arena], numbers in that order, or by name: strategy=tight min=2v1 max=5v5 distance=10 teleport=late",
            0, DrillOption.Layout, DrillOption.Min, DrillOption.Max, DrillOption.Distance, DrillOption.LineDistance, DrillOption.Spacing, DrillOption.Teleport);

        // Everyone on the field fights.
        public bool ClearsLeftovers => false;

        public bool RunsInParallel(DrillOptions options) => false;

        public Headcount Needs(DrillOptions options) => Headcount.Everyone;

        public bool Validate(DrillOptions options, out string error)
        {
            if (!_kit.HasArena(options, out error)) return false;

            PopcornRequest request = RequestFor(options);
            if (request.Smallest.Total > request.Largest.Total) error = $"The smallest fight ({request.Smallest}) is bigger than the largest ({request.Largest}).";
            else if (IsTooTight(request)) error = $"distance, lineDistance and spacing must each be at least {Numbers.Format(DrillKit.MinimumSpacing)} m.";
            else if (_kit.Players.Attacking.Count == 0 || _kit.Players.Defending.Count == 0) error = "Both teams need players for popcorn.";
            return error.Length == 0;
        }

        // The fights are dealt again at the move, for whoever is on the field by then.
        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            error = _countdown.IsRunning ? _countdown.Busy : string.Empty;
            PopcornRequest request = RequestFor(options);
            if (error.Length > 0 || !TryDraw(request, out Draw draw, out error)) return false;

            var started = new DrillRun(Name, _kit.ArenaFor(options));
            started.Note = NoteFor(started.Arena.Number, request, draw);
            var timing = new DrillTiming(Title, true, GodModeAfterMoveSeconds) { Teleport = options.GetTeleport(TeleportTime.Early) };
            _countdown.Start(timing, started, () => Place(started, request));
            run = started;
            return true;
        }

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            preview = null;
            PopcornRequest request = RequestFor(options);
            if (!TryDraw(request, out Draw draw, out error)) return false;

            preview = new DrillPreview { IsSample = true, Detail = $"{draw.LayoutName}, {draw.Fights.Count} fights: {draw.Sizes}" };
            preview.Note = NoteFor(_kit.ArenaFor(options).Number, request, draw);
            preview.AddSpots(_kit.Field(request.Scope), draw.Plan);
            return true;
        }

        private static bool IsTooTight(PopcornRequest request)
        {
            return request.Distance < DrillKit.MinimumSpacing || request.LineDistance < DrillKit.MinimumSpacing || request.Spacing < DrillKit.MinimumSpacing;
        }

        private static string NoteFor(int arena, PopcornRequest request, Draw draw)
        {
            if (draw.Distance >= request.Distance) return string.Empty;
            return FitNote.Of(arena, new[] { FitNote.Change("distance", request.Distance, draw.Distance) });
        }

        private void Place(DrillRun run, PopcornRequest request)
        {
            TryDraw(request, out Draw draw, out string error);
            int moved = _kit.Teleporter.MoveAll(draw.Plan.Players, draw.Plan.Spots);
            run.Placed(_kit.Field(request.Scope), draw.Plan.Spots);
            _kit.Log.Info($"popcorn ({draw.LayoutName}) in arena {run.Arena.Number}: {draw.Fights.Count} fights ({draw.Sizes}); {moved} of {draw.Plan.Total} moved. {NoteFor(run.Arena.Number, request, draw)}".TrimEnd());
            if (error.Length > 0) _kit.Warn(run, $"{error} The rest stay where they are.");
        }

        // A deal that doesn't fit at the asked distance takes the largest distance that does.
        private bool TryDraw(PopcornRequest request, out Draw draw, out string error)
        {
            List<Lineup> fights = _fights.Deal(_kit.Field(request.Scope), request.Smallest, request.Largest);
            draw = DrawAt(fights, request);
            if (!draw.Plan.PlacesEveryone) draw = Closest(fights, request);

            SpawnPlan plan = draw.Plan;
            error = plan.PlacesEveryone ? string.Empty : $"The arena has room for {plan.Spots.Count} of {plan.Total} players in {fights.Count} fights, even at distance {Numbers.Format(DrillKit.MinimumSpacing)}: lower lineDistance or spacing, or use a bigger arena.";
            return error.Length == 0;
        }

        private Draw DrawAt(List<Lineup> fights, PopcornRequest request)
        {
            return new Draw(fights, SpawnPlan.Fullest(() => _layout.Place(fights, request)), request);
        }

        // Starts at the smallest distance, then halves the gap between the largest that fit and the smallest that didn't.
        private Draw Closest(List<Lineup> fights, PopcornRequest request)
        {
            Draw best = DrawAt(fights, request.WithDistance(DrillKit.MinimumSpacing));
            float fits = DrillKit.MinimumSpacing;
            float fails = request.Distance;
            for (int step = 0; step < SearchSteps && best.Plan.PlacesEveryone; step++)
            {
                float middle = Numbers.RoundDown((fits + fails) * Half);
                Draw draw = DrawAt(fights, request.WithDistance(middle));
                if (draw.Plan.PlacesEveryone) (best, fits) = (draw, middle);
                else fails = middle;
            }
            return best;
        }

        private PopcornRequest RequestFor(DrillOptions options)
        {
            return new PopcornRequest
            {
                Layout = _layout.Resolve(options.GetFightLayout(_kit.Settings.PopcornStrategy.Value)),
                Smallest = options.GetFightSize(DrillOption.Min, _kit.Settings.PopcornMin.Value),
                Largest = options.GetFightSize(DrillOption.Max, _kit.Settings.PopcornMax.Value),
                Area = _kit.ArenaFor(options).Bounds,
                Distance = options.GetNumber(DrillOption.Distance, _kit.Settings.PopcornDistance.Value),
                LineDistance = options.GetNumber(DrillOption.LineDistance, _kit.Settings.PopcornLineDistance.Value),
                Spacing = options.GetNumber(DrillOption.Spacing, _kit.Settings.PopcornSpacing.Value),
                Scope = options.Scope,
            };
        }

        private sealed class Draw
        {
            public Draw(List<Lineup> fights, SpawnPlan plan, PopcornRequest used)
            {
                Fights = fights;
                Plan = plan;
                LayoutName = used.Layout.ToString().ToLowerInvariant();
                Distance = used.Distance;
            }

            public string LayoutName { get; }

            public float Distance { get; }

            public List<Lineup> Fights { get; }

            public SpawnPlan Plan { get; }

            public string Sizes => string.Join(", ", Fights.Select(fight => $"{fight.Attackers.Count}v{fight.Defenders.Count}"));
        }
    }
}
