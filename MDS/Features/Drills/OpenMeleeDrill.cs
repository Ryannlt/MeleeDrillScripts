using System.Collections;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    public sealed class OpenMeleeDrill
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

        public static Rect Area(Arena arena, float offset)
        {
            Vector2 min = arena.Min + Vector2.one * offset;
            Vector2 max = arena.Max - Vector2.one * offset;
            return min.x < max.x && min.y < max.y ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : Rect.zero;
        }

        // The spots are drawn again at the move, for whoever is on the field by then.
        public bool CanPlace(float spacing, float offset, out string error)
        {
            error = Refusal(spacing, offset);
            return error.Length == 0;
        }

        public void Start(float spacing, float offset) => _running = _routines.Start(Run(spacing, offset), "open melee");

        // The size checks come before any spot is drawn: a tiny spacing over a big arena needs a grid too large to build.
        private string Refusal(float spacing, float offset)
        {
            int players = Field().Count;
            Rect area = Area(_kit.Arenas.Current, offset);
            if (_running != null && !_running.IsFinished) return "An open melee is already counting down.";
            if (players == 0) return "Nobody is on the field for an open melee.";
            if (spacing < MinimumSpacing) return $"Spacing must be at least {Numbers.Format(MinimumSpacing)} m.";
            if (!PoissonGrid.Fits(area, spacing)) return "The arena is too big for that spacing.";

            int spots = area.width > 0f ? _sampler.Generate(area, spacing, players).Count : 0;
            if (spots >= players) return string.Empty;

            return $"The arena has room for {spots} of {players} players {Numbers.Format(spacing)} m apart and {Numbers.Format(offset)} m in from the edges.";
        }

        private IEnumerator Run(float spacing, float offset)
        {
            for (int left = CountdownSeconds; left > 0; left--)
            {
                _kit.Console.Execute(left == CountdownSeconds ? $"broadcast Open Melee starting in {left}..." : $"broadcast {left}...");
                yield return Routines.Wait(CountdownStepSeconds);
            }

            _kit.Console.ExecuteForCurrentRound("set characterGodMode true");
            Scatter(spacing, offset);
            yield return Routines.Wait(GodModeAfterMoveSeconds);
            _kit.Console.ExecuteForCurrentRound("set characterGodMode false");
        }

        private void Scatter(float spacing, float offset)
        {
            List<IPlayer> players = Field();
            List<Vector2> spots = _sampler.Generate(Area(_kit.Arenas.Current, offset), spacing, players.Count);
            int moved = _kit.Teleporter.MoveAll(players, spots);
            _kit.Log.Info($"openmelee: {players.Count} on the field, {spots.Count} spots, {moved} moved.");
        }

        private List<IPlayer> Field()
        {
            var field = new List<IPlayer>(_kit.Players.Attacking);
            field.AddRange(_kit.Players.Defending);
            return field;
        }
    }
}
