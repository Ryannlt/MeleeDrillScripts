using System.Collections;
using System.Collections.Generic;
using MDS.Features.Arenas;
using System.Linq;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Preview;
using MDS.Features.Drills.Spawning;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    public sealed class OpenMeleeDrill : IDrill
    {
        private const int CountdownSeconds = 3;
        private const float CountdownStepSeconds = 1f;
        private const float GodModeAfterMoveSeconds = 2f;

        // About a player's width.
        // Below it players would stand inside each other.
        private const float MinimumSpacing = 0.5f;

        // A random layout near the arena's capacity can come up short, so a few are drawn and the fullest kept.
        private const int PlanAttempts = 5;

        private readonly DrillKit _kit;
        private readonly Routines _routines;
        private readonly SpawnPatterns _patterns;
        private Routine _running;

        public OpenMeleeDrill(DrillKit kit, Routines routines)
        {
            _kit = kit;
            _routines = routines;
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
            error = _running != null && !_running.IsFinished ? "An open melee is already counting down." : Refusal(request, out _);
            if (error.Length > 0) return false;

            run = new DrillRun(Name, _kit.ArenaFor(options));
            _running = _routines.Start(Run(run, request), "open melee");
            return true;
        }

        public bool TryPreview(DrillOptions options, out DrillPreview preview, out string error)
        {
            preview = null;
            SpawnRequest request = RequestFor(options);
            error = Refusal(request, out SpawnPlan plan);
            if (error.Length > 0) return false;

            preview = PreviewOf(Field(_kit.Players), plan, request);
            return true;
        }

        private static Lineup Field(PlayerRegistry players) => new Lineup(new List<IPlayer>(players.Attacking), new List<IPlayer>(players.Defending));

        private static string NameOf(SpawnStrategy strategy) => strategy.ToString().ToLowerInvariant();

        private static DrillPreview PreviewOf(Lineup field, SpawnPlan plan, SpawnRequest request)
        {
            var preview = new DrillPreview { Area = request.Area, IsSample = true, Detail = NameOf(request.Strategy) };
            for (int i = 0; i < plan.Players.Count; i++) (field.Attackers.Contains(plan.Players[i]) ? preview.Attackers : preview.Defenders).Add(plan.Spots[i]);
            return preview;
        }

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
            if (request.Spacing < MinimumSpacing) return $"Spacing must be at least {Numbers.Format(MinimumSpacing)} m.";
            // Qualified, since the offline checker would otherwise bind the stale MDS.Features.Drills.PoissonGrid in HBox's last editor build.
            if (!Spawning.PoissonGrid.Fits(request.Area, request.Spacing)) return "The arena is too big for that spacing.";

            if (request.Area.width > 0f) plan = FullestPlan(Field(_kit.Players), request);
            if (plan.Spots.Count >= players) return string.Empty;

            return $"The arena has room for {plan.Spots.Count} of {players} players {Numbers.Format(request.Spacing)} m apart and {Numbers.Format(request.Offset)} m in from the edges.";
        }

        private IEnumerator Run(DrillRun run, SpawnRequest request)
        {
            for (int left = CountdownSeconds; left > 0; left--)
            {
                _kit.Console.Execute(left == CountdownSeconds ? $"broadcast Open Melee starting in {left}..." : $"broadcast {left}...");
                yield return Routines.Wait(CountdownStepSeconds);
            }

            _kit.Console.ExecuteForCurrentRound("set characterGodMode true");
            Place(run, request);
            yield return Routines.Wait(GodModeAfterMoveSeconds);
            _kit.Console.ExecuteForCurrentRound("set characterGodMode false");
        }

        private void Place(DrillRun run, SpawnRequest request)
        {
            Lineup field = Field(_kit.Players);
            SpawnPlan plan = FullestPlan(field, request);
            int moved = _kit.Teleporter.MoveAll(plan.Players, plan.Spots);
            run.Placed(field, plan.Spots);
            _kit.Log.Info($"openmelee ({NameOf(request.Strategy)}) in arena {run.Arena.Number}: {plan.Total} on the field, {plan.Spots.Count} spots, {moved} moved.");
            if (!plan.PlacesEveryone) _kit.Log.Warning($"openmelee: no room for {plan.Unplaced} of {plan.Total} players, who stay where they are.");
        }

        private SpawnPlan FullestPlan(Lineup field, SpawnRequest request)
        {
            ISpawnPattern pattern = _patterns.For(request.Strategy);
            SpawnPlan fullest = pattern.Plan(field, request);
            for (int attempt = 1; attempt < PlanAttempts && !fullest.PlacesEveryone; attempt++)
            {
                SpawnPlan next = pattern.Plan(field, request);
                if (next.Spots.Count > fullest.Spots.Count) fullest = next;
            }
            return fullest;
        }
    }
}
