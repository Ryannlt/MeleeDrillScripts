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
        private readonly OpenMeleePlanner _planner;

        public OpenMeleeDrill(DrillKit kit, DrillCountdown countdown)
        {
            _kit = kit;
            _countdown = countdown;
            _planner = new OpenMeleePlanner(kit);
        }

        public string Name => "openmelee";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc openmelee [scatter|spread|groups|teams|random] [spacing] [offset] [facing] [arena], numbers in that order, or by name: strategy=spread spacing=2 facing=90",
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
            SpawnRequest request = _planner.RequestFor(options, _kit.ArenaFor(options));
            error = _countdown.IsRunning ? _countdown.Busy : _planner.Refusal(request, _kit.Field(), out _);
            if (error.Length > 0) return false;

            DrillRun started = new DrillRun(Name, _kit.ArenaFor(options));
            _countdown.Start(new DrillTiming(Title, true, GodModeAfterMoveSeconds), () => Place(started, request));
            run = started;
            return true;
        }

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            preview = null;
            SpawnRequest request = _planner.RequestFor(options, _kit.ArenaFor(options));
            error = _planner.Refusal(request, _kit.Field(), out SpawnPlan plan);
            if (error.Length > 0) return false;

            preview = new DrillPreview { Area = request.Area, IsSample = true, Detail = OpenMeleePlanner.NameOf(request.Strategy) };
            preview.AddSpots(_kit.Field(), plan);
            return true;
        }

        private void Place(DrillRun run, SpawnRequest request)
        {
            Lineup field = _kit.Field();
            SpawnPlan plan = _planner.FullestPlan(field, request);
            int moved = _kit.Teleporter.MoveAll(plan.Players, plan.Spots);
            run.Placed(field, plan.Spots);
            _kit.Log.Info($"openmelee ({OpenMeleePlanner.NameOf(request.Strategy)}) in arena {run.Arena.Number}: {plan.Total} on the field, {plan.Spots.Count} spots, {moved} moved.");
            if (!plan.PlacesEveryone) _kit.Warn(run, $"no room for {plan.Unplaced} of {plan.Total} players at the move, so they stay where they are.");
        }
    }
}
