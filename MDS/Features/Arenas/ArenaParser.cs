using System;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Arenas
{
    // Coordinates the way config lines and commands write them: a corner "x,z" (or "x z" as two arguments), an arena
    // "(x,z),(x,z)".
    public static class ArenaParser
    {
        private const string BetweenCorners = "),(";

        // Two numbers make a corner, two corners an arena.
        private const int Pair = 2;

        public static bool TryParseCorner(string text, out Vector2 corner)
        {
            corner = default;
            string[] parts = (text ?? string.Empty).Trim().Trim('(', ')').Split(',');
            if (parts.Length != Pair || !Numbers.TryParse(parts[0], out float x) || !Numbers.TryParse(parts[1], out float z)) return false;

            corner = new Vector2(x, z);
            return true;
        }

        public static bool TryParseCorner(string[] args, out Vector2 corner)
        {
            if (args.Length == 1) return TryParseCorner(args[0], out corner);

            corner = default;
            return args.Length == Pair && TryParseCorner($"{args[0]},{args[1]}", out corner);
        }

        public static bool TryParseArena(string text, out Arena arena)
        {
            arena = null;
            string[] corners = (text ?? string.Empty).Replace(" ", string.Empty).Split(new[] { BetweenCorners }, StringSplitOptions.None);
            if (corners.Length != Pair || !TryParseCorner(corners[0], out Vector2 first) || !TryParseCorner(corners[1], out Vector2 second)) return false;

            arena = new Arena(first, second);
            return true;
        }
    }
}
