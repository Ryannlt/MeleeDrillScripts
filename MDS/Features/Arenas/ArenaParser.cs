using System;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Arenas
{
    public static class ArenaParser
    {
        private const string BetweenCorners = "),(";
        private const char AfterNumber = ':';

        private const int Pair = 2;

        public static bool TryParseNumber(string text, out int number) => int.TryParse(text, out number) && number >= ArenaStore.First;

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

        // "(x,z),(x,z)" is arena 1, and "2:(x,z),(x,z)" is arena 2.
        public static bool TryParseArena(string text, out Arena arena)
        {
            arena = null;
            if (!TrySplitNumber(text, out int number, out string rest)) return false;

            string[] corners = rest.Replace(" ", string.Empty).Split(new[] { BetweenCorners }, StringSplitOptions.None);
            if (corners.Length != Pair || !TryParseCorner(corners[0], out Vector2 first) || !TryParseCorner(corners[1], out Vector2 second)) return false;

            arena = new Arena(number, first, second);
            return true;
        }

        // "x,z" is a corner of arena 1, and "2:x,z" a corner of arena 2.
        public static bool TryParseNumberedCorner(string text, out int number, out Vector2 corner)
        {
            corner = default;
            return TrySplitNumber(text, out number, out string rest) && TryParseCorner(rest, out corner);
        }

        private static bool TrySplitNumber(string text, out int number, out string rest)
        {
            rest = text ?? string.Empty;
            number = ArenaStore.First;
            int split = rest.IndexOf(AfterNumber);
            if (split < 0) return true;

            bool valid = TryParseNumber(rest.Substring(0, split).Trim(), out number);
            rest = rest.Substring(split + 1);
            return valid;
        }
    }
}
