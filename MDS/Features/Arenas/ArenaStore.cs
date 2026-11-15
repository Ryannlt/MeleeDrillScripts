using System;
using System.Collections.Generic;
using MDS.Shared.Logging;
using UnityEngine;

namespace MDS.Features.Arenas
{
    public enum ArenaCorner
    {
        Corner1,
        Corner2
    }

    // The mod reloads with every map, so nothing here resets per round.
    public sealed class ArenaStore
    {
        public const int First = 1;

        private readonly SortedDictionary<int, Arena> _arenas = new();
        private readonly ModLog _log;

        public ArenaStore(ModLog log)
        {
            _log = log;
        }

        public event Action<Arena> ArenaChanged;

        public IEnumerable<Arena> All => _arenas.Values;

        public static string NotSet(int number)
        {
            return number == First
                ? "No arena is set: use rc arena corner1 and corner2, or MDS:Arena in the config."
                : $"Arena {number} isn't set: use rc arena {number} corner1 and corner2, or MDS:Arena:{number}: in the config.";
        }

        public Arena Find(int number) => _arenas.TryGetValue(number, out Arena arena) ? arena : null;

        public bool TryGetUsable(int number, out Arena arena, out string error)
        {
            arena = Find(number);
            bool usable = arena != null && arena.IsUsable;
            error = usable ? string.Empty : NotSet(number);
            return usable;
        }

        public void Set(Arena arena)
        {
            _arenas[arena.Number] = arena;
            _log.Info($"{arena}.");
            ArenaChanged?.Invoke(arena);
        }

        public void SetCorner(int number, ArenaCorner corner, Vector2 position)
        {
            Arena current = Find(number) ?? new Arena(number, position, position);
            Set(corner == ArenaCorner.Corner1 ? new Arena(number, position, current.Corner2) : new Arena(number, current.Corner1, position));
        }

        public string Describe()
        {
            if (_arenas.Count == 0) return NotSet(First);

            var lines = new List<string>();
            foreach (Arena arena in _arenas.Values) lines.Add($"{arena}.");
            return string.Join(" ", lines);
        }
    }
}
