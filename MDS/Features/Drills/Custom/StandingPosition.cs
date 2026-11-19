using System;
using MDS.Features.Arenas;
using MDS.Shared;
using MDS.Shared.Game;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // "here" in a custom drill line is where the admin stands, in meters from the center of the arena they stand in.
    // In nested arenas the smallest counts, since a drill area drawn inside a big arena is the one meant.
    public sealed class StandingPosition
    {
        private const string HereWord = "here";

        // Any position, so a line with "here" can be checked before anyone's position is read.
        private const string Placeholder = "(0,0)";

        private readonly ArenaStore _arenas;
        private readonly PlayerQueries _queries;

        public StandingPosition(ArenaStore arenas, PlayerQueries queries)
        {
            _arenas = arenas;
            _queries = queries;
        }

        public static string WithPlaceholder(string line) => Replace(line, Placeholder);

        // The line with every here replaced, or null when the error says why it can't be.
        public string Resolve(int playerId, string line, out string error)
        {
            error = string.Empty;
            if (!Array.Exists(CommaParts.Split(line), IsHere)) return line;

            error = ReadOffset(playerId, out Vector2 offset);
            return error.Length > 0 ? null : Replace(line, $"({Numbers.Coordinate(offset.x)},{Numbers.Coordinate(offset.y)})");
        }

        private string ReadOffset(int playerId, out Vector2 offset)
        {
            offset = Vector2.zero;
            if (!_queries.TryGetPosition(playerId, out Vector3 standing)) return "Your position can't be read. Give a position like (-5,0) instead of here.";

            var flat = new Vector2(standing.x, standing.z);
            Arena arena = SmallestAround(flat);
            if (arena == null) return "You aren't standing in an arena. Stand inside one, or give a position like (-5,0).";

            offset = flat - arena.Center;
            return string.Empty;
        }

        private static bool IsHere(string part) => part.Equals(HereWord, StringComparison.OrdinalIgnoreCase);

        private static string Replace(string line, string position) => string.Join(",", Array.ConvertAll(CommaParts.Split(line), part => IsHere(part) ? position : part));

        private Arena SmallestAround(Vector2 position)
        {
            Arena smallest = null;
            foreach (Arena arena in _arenas.All)
            {
                if (!arena.IsUsable || !arena.Bounds.Contains(position)) continue;
                if (smallest == null || Area(arena) < Area(smallest)) smallest = arena;
            }
            return smallest;
        }

        private static float Area(Arena arena) => arena.Bounds.width * arena.Bounds.height;
    }
}
