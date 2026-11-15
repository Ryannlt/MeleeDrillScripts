using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;
using MDS.Features.Drills.Spawning;
using MDS.Shared;
using MDS.Shared.Game;
using UnityEngine;

namespace MDS.Features.Drills
{
    public sealed class OpenMeleeDrill : IDrill
    {
        private const string Title = "Open Melee";
        private const float GodModeAfterMoveSeconds = 2f;

        private readonly DrillKit _kit;
        private readonly DrillCountdown _countdown;
        private readonly SpawnPatterns _patterns;

        public OpenMeleeDrill(DrillKit kit, DrillCountdown countdown)
        {
            _kit = kit;
            _countdown = countdown;
            _patterns = new SpawnPatterns(kit.Random);
        }

        public string Name => "openmelee";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc openmelee [scatter|spread|groups|teams|random] [spacing] [offset] [facing] [arena=N], or by name: strategy=spread spacing=2 offset=3 facing=random",
            0, DrillOption.Spawn, DrillOption.Spacing, DrillOption.Offset, DrillOption.Facing);

        // Everyone on the field is placed.
        public bool ClearsLeftovers => false;

        public static Rect Area(Arena arena, float offset)
        {
            Vector2 min = arena.Min + Vector2.one * offset;
            Vector2 max = arena.Max - Vector2.one * offset;
            return min.x < max.x && min.y < max.y ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : Rect.zero;
        }

        public bool Validate(DrillOptions options, out string error) => _kit.HasArena(options, out error);

        // The spots are drawn again at the move, for whoever is on the field by then.
        public bool TryStart(DrillOptions options, out DrillRun run, out string error)
        {
            run = null;
            SpawnRequest request = RequestFor(options);
            error = _countdown.IsRunning ? _countdown.Busy : Refusal(request, out _);
            if (error.Length > 0) return false;

            DrillRun started = new DrillRun(Name, _kit.ArenaFor(options));
            _countdown.Start(Title, () => Place(started, request), GodModeAfterMoveSeconds);
            run = started;
            return true;
        }

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            preview = null;
            SpawnRequest request = RequestFor(options);
            error = Refusal(request, out SpawnPlan plan);
            if (error.Length > 0) return false;

            preview = new DrillPreview { Area = request.Area, IsSample = true, Detail = NameOf(request.Strategy) };
            preview.AddSpots(_kit.Field(), plan);
            return true;
        }

        private static string NameOf(SpawnStrategy strategy) => strategy.ToString().ToLowerInvariant();

        private SpawnRequest RequestFor(DrillOptions options)
        {
            float offset = options.GetNumber(DrillOption.Offset, _kit.Settings.OpenMeleeOffset.Value);
            return new SpawnRequest
            {
                Strategy = _patterns.Resolve(options.GetSpawnStrategy(_kit.Settings.OpenMeleeStrategy.Value)),
                Area = Area(_kit.ArenaFor(options), offset),
                Offset = offset,
                Spacing = options.GetNumber(DrillOption.Spacing, _kit.Settings.OpenMeleeSpacing.Value),
                Degrees = options.GetFacing(_kit.Settings.Orientation.Value).Resolve(_kit.Random),
            };
        }

        // The size checks come before any spot is drawn: a tiny spacing over a big arena needs a grid too large to build.
        private string Refusal(SpawnRequest request, out SpawnPlan plan)
        {
            plan = new SpawnPlan();
            int players = _kit.Players.Attacking.Count + _kit.Players.Defending.Count;
            if (players == 0) return "Nobody is on the field for an open melee.";
            if (request.Spacing < DrillKit.MinimumSpacing) return $"Spacing must be at least {Numbers.Format(DrillKit.MinimumSpacing)} m.";
            // Qualified, since the offline checker would otherwise bind the stale MDS.Features.Drills.PoissonGrid in HBox's last editor build.
            if (!Spawning.PoissonGrid.Fits(request.Area, request.Spacing)) return "The arena is too big for that spacing.";

            if (request.Area.width > 0f) plan = FullestPlan(_kit.Field(), request);
            if (plan.Spots.Count >= players) return string.Empty;

            return $"The arena has room for {plan.Spots.Count} of {players} players {Numbers.Format(request.Spacing)} m apart and {Numbers.Format(request.Offset)} m in from the edges.";
        }

        private void Place(DrillRun run, SpawnRequest request)
        {
            Lineup field = _kit.Field();
            SpawnPlan plan = FullestPlan(field, request);
            int moved = _kit.Teleporter.MoveAll(plan.Players, plan.Spots);
            run.Placed(field, plan.Spots);
            _kit.Log.Info($"openmelee ({NameOf(request.Strategy)}) in arena {run.Arena.Number}: {plan.Total} on the field, {plan.Spots.Count} spots, {moved} moved.");
            if (!plan.PlacesEveryone) _kit.Log.Warning($"openmelee: no room for {plan.Unplaced} of {plan.Total} players, who stay where they are.");
        }

        private SpawnPlan FullestPlan(Lineup field, SpawnRequest request)
        {
            ISpawnPattern pattern = _patterns.For(request.Strategy);
            return SpawnPlan.Fullest(() => pattern.Plan(field, request));
        }
    }
}
