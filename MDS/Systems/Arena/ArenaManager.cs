using System.Collections.Generic;
using UnityEngine;

namespace MDS.Systems
{
    public static class ArenaManager
    {
        private static readonly List<Arena> _arenas = new();
        private static readonly List<Arena> _stagedArenas = new(); // Used until round info arrives
        private static bool _defaultArenaStaged = false;

        public static Arena Arena => _arenas.Count > 0 ? _arenas[0] : null;
        public static IReadOnlyList<Arena> AllArenas => _arenas.AsReadOnly();

        public static void StageArena(Vector2 corner1, Vector2 corner2, bool isDefault)
        {
            if (isDefault)
            {
                if (_arenas.Count == 0)
                    _arenas.Add(new Arena(corner1, corner2));
                else
                    _arenas[0] = new Arena(corner1, corner2);

                _defaultArenaStaged = true;
                Logger.Log($"Staged default arena: {_arenas[0]}", LogLevel.DEBUG);
            }
            else
            {
                if (!_defaultArenaStaged)
                {
                    _arenas.Insert(0, null); // placeholder for default
                    _defaultArenaStaged = true;
                    Logger.Log("Inserted placeholder for default arena before appending others.", LogLevel.DEBUG);
                }

                var arena = new Arena(corner1, corner2);
                _arenas.Add(arena);
                Logger.Log($"Staged additional arena: {arena}", LogLevel.DEBUG);
            }
        }

        public static void ApplyStagedArenas()
        {
            if (_stagedArenas.Count > 0)
            {
                _arenas.Clear();
                _arenas.AddRange(_stagedArenas);
                _stagedArenas.Clear();

                Logger.Log($"Staged arenas applied. Count: {_arenas.Count}", LogLevel.INFO);
            }
        }

        public static void AddArena(Vector2 corner1, Vector2 corner2)
        {
            _arenas.Add(new Arena(corner1, corner2));
        }

        public static void SetArenaAt(int index, Vector2 corner1, Vector2 corner2)
        {
            if (index < 0 || index >= _arenas.Count)
            {
                Logger.Log($"Arena index {index} is out of bounds.", LogLevel.WARNING);
                return;
            }

            _arenas[index] = new Arena(corner1, corner2);
            Logger.Log($"Updated Arena at index {index}: {_arenas[index]}", LogLevel.INFO);
        }

        public static Arena GetArena(int index)
        {
            return (index >= 0 && index < _arenas.Count) ? _arenas[index] : null;
        }

        public static void SetArena(Vector2 a, Vector2 b)
        {
            if (_arenas.Count == 0)
                _arenas.Add(new Arena(a, b));
            else
                _arenas[0] = new Arena(a, b);

            Logger.Log($"Arena set: {_arenas[0]}", LogLevel.DEBUG);
        }

        public static void SetArenaCorner1(Vector2 corner)
        {
            if (_arenas.Count == 0)
                _arenas.Add(new Arena(corner, corner));
            else
                _arenas[0] = new Arena(corner, _arenas[0].Corner2);
        }

        public static void SetArenaCorner2(Vector2 corner)
        {
            if (_arenas.Count == 0)
                _arenas.Add(new Arena(corner, corner));
            else
                _arenas[0] = new Arena(_arenas[0].Corner1, corner);
        }

        public static void Reset()
        {
            _arenas.Clear();
            _stagedArenas.Clear();
            _defaultArenaStaged = false;
        }
    }
}
