using System.Collections;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
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

        private readonly DrillKit _kit;
        private readonly Routines _routines;
        private readonly PoissonSampler _sampler;
        private Routine _running;

        public OpenMeleeDrill(DrillKit kit, Routines routines)
        {
            _kit = kit;
            _routines = routines;
            _sampler = new PoissonSampler(kit.Random);
        }

        public string Name => "openmelee";

        public DrillSyntax Syntax { get; } = new DrillSyntax(
            "Usage: rc openmelee [spacing] [offset] [arena=N], or by name: spacing=2 offset=3", 0, DrillOption.Spacing, DrillOption.Offset);

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
            float spacing = options.GetNumber(DrillOption.Spacing, _kit.Settings.OpenMeleeSpacing.Value);
            float offset = options.GetNumber(DrillOption.Offset, _kit.Settings.OpenMeleeOffset.Value);
            Arena arena = _kit.ArenaFor(options);
            Rect area = Area(arena, offset);
            error = Refusal(area, spacing, offset);
            if (error.Length > 0) return false;

            run = new DrillRun(Name, arena);
            _running = _routines.Start(Run(run, area, spacing), "open melee");
            return true;
        }

        // The size checks come before any spot is drawn: a tiny spacing over a big arena needs a grid too large to build.
        private string Refusal(Rect area, float spacing, float offset)
        {
            int players = _kit.Players.Attacking.Count + _kit.Players.Defending.Count;
            if (_running != null && !_running.IsFinished) return "An open melee is already counting down.";
            if (players == 0) return "Nobody is on the field for an open melee.";
            if (spacing < MinimumSpacing) return $"Spacing must be at least {Numbers.Format(MinimumSpacing)} m.";
            if (!PoissonGrid.Fits(area, spacing)) return "The arena is too big for that spacing.";

            int spots = area.width > 0f ? _sampler.Generate(area, spacing, players).Count : 0;
            if (spots >= players) return string.Empty;

            return $"The arena has room for {spots} of {players} players {Numbers.Format(spacing)} m apart and {Numbers.Format(offset)} m in from the edges.";
        }

        private IEnumerator Run(DrillRun run, Rect area, float spacing)
        {
            for (int left = CountdownSeconds; left > 0; left--)
            {
                _kit.Console.Execute(left == CountdownSeconds ? $"broadcast Open Melee starting in {left}..." : $"broadcast {left}...");
                yield return Routines.Wait(CountdownStepSeconds);
            }

            _kit.Console.ExecuteForCurrentRound("set characterGodMode true");
            Scatter(run, area, spacing);
            yield return Routines.Wait(GodModeAfterMoveSeconds);
            _kit.Console.ExecuteForCurrentRound("set characterGodMode false");
        }

        private void Scatter(DrillRun run, Rect area, float spacing)
        {
            var lineup = new Lineup(new List<IPlayer>(_kit.Players.Attacking), new List<IPlayer>(_kit.Players.Defending));
            var field = new List<IPlayer>(lineup.Attackers);
            field.AddRange(lineup.Defenders);
            List<Vector2> spots = _sampler.Generate(area, spacing, field.Count);
            int moved = _kit.Teleporter.MoveAll(field, spots);
            run.Placed(lineup, spots);
            _kit.Log.Info($"openmelee in arena {run.Arena.Number}: {field.Count} on the field, {spots.Count} spots, {moved} moved.");
        }
    }
}
