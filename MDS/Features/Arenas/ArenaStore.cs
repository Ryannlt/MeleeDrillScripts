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

    // The arenas on this map. Drills use the first; more can be listed for later multi-arena drills. The mod reloads
    // with every map, so the config sets them at load and rc arena changes them while the map runs.
    public sealed class ArenaStore
    {
        public const string NotSet = "No arena is set: use rc arena corner1 and corner2, or MDS:Arena in the config.";

        private readonly List<Arena> _arenas = new();
        private readonly ModLog _log;

        public ArenaStore(ModLog log)
        {
            _log = log;
        }

        // Null until set.
        public Arena Current => _arenas.Count > 0 ? _arenas[0] : null;

        public IReadOnlyList<Arena> All => _arenas;

        public void Set(Arena arena)
        {
            if (_arenas.Count == 0) _arenas.Add(arena);
            else _arenas[0] = arena;
            _log.Info($"Arena set: {arena}.");
        }

        public void Add(Arena arena)
        {
            _arenas.Add(arena);
            _log.Info($"Arena {_arenas.Count} added: {arena}.");
        }

        // A first corner on its own makes a one-point arena, which the other corner then stretches.
        public void SetCorner(ArenaCorner corner, Vector2 position)
        {
            Arena current = Current ?? new Arena(position, position);
            Set(corner == ArenaCorner.Corner1 ? new Arena(position, current.Corner2) : new Arena(current.Corner1, position));
        }
    }
}
