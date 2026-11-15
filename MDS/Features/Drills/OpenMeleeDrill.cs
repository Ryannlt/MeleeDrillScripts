using System.Collections;
using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Shared;
using MDS.Shared.Game;
using MDS.Shared.Players;
using UnityEngine;

namespace MDS.Features.Drills
{
    // rc openmelee: a 3-2-1 countdown, then everyone on the field scattered over the arena, at least `spacing` apart and
    // at least `offset` in from its edges. God mode is on from just before the move until 2 s after it.
    public sealed class OpenMeleeDrill
    {
        private const int CountdownSeconds = 3;
        private const float CountdownStepSeconds = 1f;
        private const float GodModeAfterMoveSeconds = 2f;

        private readonly DrillKit _kit;
        private readonly Routines _routines;
        private readonly PoissonSampler _sampler;

        public OpenMeleeDrill(DrillKit kit, Routines routines)
        {
            _kit = kit;
            _routines = routines;
            _sampler = new PoissonSampler(kit.Random);
        }

        // The arena shrunk by the offset on every side. Empty when that leaves no room.
        public static Rect Area(Arena arena, float offset)
        {
            Vector2 min = arena.Min + Vector2.one * offset;
            Vector2 max = arena.Max - Vector2.one * offset;
            return min.x < max.x && min.y < max.y ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : Rect.zero;
        }

        // Whether everyone on the field would get a spot now. The spots are drawn again at the move, for whoever is
        // on the field then.
        public bool CanPlace(float spacing, float offset, out string error)
        {
            int players = Field().Count;
            Rect area = Area(_kit.Arenas.Current, offset);
            int spots = area.width > 0f ? _sampler.Generate(area, spacing, players).Count : 0;
            if (players == 0) error = "Nobody is on the field for an open melee.";
            else if (spots < players) error = $"The arena has room for {spots} of {players} players {Numbers.Format(spacing)} m apart and {Numbers.Format(offset)} m in from the edges.";
            else error = string.Empty;
            return error.Length == 0;
        }

        public void Start(float spacing, float offset) => _routines.Start(Run(spacing, offset), "open melee");

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
