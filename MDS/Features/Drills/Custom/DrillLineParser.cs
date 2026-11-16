using System;
using MDS.Features.Drills.Picking;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Drills.Custom
{
    // Reads "attacking 2 at -5,0 facing eastwest spacing 2 line", "strategy next" or "turn 90".
    public static class DrillLineParser
    {
        public const string GroupFormat = "<attacking|defending> <count> at <x>,<z> [facing <direction>] [spacing <m>] [line|scatter]";

        // More than any server holds, for a group and for each side of a drill.
        public const int MostPlayers = 100;
        private const float DefaultSpacing = 2f;
        private const int SettingWords = 2;
        private const int SideWord = 0;
        private const int CountWord = 1;
        private const int AtWord = 2;
        private const int OffsetWord = 3;
        private const int HeadWords = 4;
        private const int OffsetParts = 2;
        private static readonly char[] Blanks = { ' ', '\t' };

        public static bool TryParse(string text, out DrillLine line, out string error)
        {
            string[] words = (text ?? string.Empty).Split(Blanks, StringSplitOptions.RemoveEmptyEntries);
            line = new DrillLine();
            if (words.Length == SettingWords && Is(words[0], "strategy")) error = ReadStrategy(words[1], line);
            else if (words.Length == SettingWords && Is(words[0], "turn")) error = ReadTurn(words[1], line);
            else error = ReadGroup(words, line);
            return error.Length == 0;
        }

        private static bool Is(string word, string expected) => string.Equals(word, expected, StringComparison.OrdinalIgnoreCase);

        private static string ReadStrategy(string word, DrillLine line)
        {
            if (!EnumParser.TryParseStrict(word, out PickStrategy strategy)) return $"'{word}' isn't a strategy: use random, any, next or repeat.";

            line.Strategy = strategy;
            return string.Empty;
        }

        private static string ReadTurn(string word, DrillLine line)
        {
            if (!Facing.TryParse(word, out Facing turn)) return $"'{word}' isn't a turn: use 0 to 359 degrees or random.";

            line.Turn = turn;
            return string.Empty;
        }

        private static string ReadGroup(string[] words, DrillLine line)
        {
            var group = new DrillGroup { Spacing = DefaultSpacing, Shape = GroupShape.Line, Text = string.Join(" ", words) };
            string error = ReadHead(words, group);
            for (int i = HeadWords; i < words.Length && error.Length == 0; i++) error = ReadOption(words, ref i, group);
            if (error.Length > 0) return $"{error} A group reads: {GroupFormat}";

            line.Group = group;
            return string.Empty;
        }

        private static string ReadHead(string[] words, DrillGroup group)
        {
            if (words.Length < HeadWords || !Is(words[AtWord], "at")) return "Expected '<side> <count> at <x>,<z>'.";
            if (!EnumParser.TryParseStrict(words[SideWord], out TeamSide side)) return $"'{words[SideWord]}' isn't attacking or defending.";
            if (!int.TryParse(words[CountWord], out int count) || count < 1 || count > MostPlayers) return $"'{words[CountWord]}' isn't a count from 1 to {MostPlayers}.";
            if (!TryParseOffset(words[OffsetWord], out Vector2 offset)) return $"'{words[OffsetWord]}' isn't a position like -5,0.";

            group.Side = side;
            group.Count = count;
            group.Offset = offset;
            return string.Empty;
        }

        // A shape word on its own, or facing and spacing each with the word after them.
        private static string ReadOption(string[] words, ref int i, DrillGroup group)
        {
            string word = words[i];
            if (EnumParser.TryParseStrict(word, out GroupShape shape))
            {
                group.Shape = shape;
                return string.Empty;
            }

            if (!Is(word, "facing") && !Is(word, "spacing")) return $"'{word}' isn't part of a group: use facing, spacing, line or scatter.";
            if (i + 1 >= words.Length) return $"{word} needs a value.";

            string value = words[++i];
            return Is(word, "facing") ? SetFacing(value, group) : SetSpacing(value, group);
        }

        private static string SetFacing(string value, DrillGroup group)
        {
            if (!Facing.TryParse(value, out Facing facing)) return $"'{value}' isn't a facing: use northsouth, eastwest, southnorth, westeast, 0 to 359 or random.";

            group.Facing = facing;
            return string.Empty;
        }

        private static string SetSpacing(string value, DrillGroup group)
        {
            if (!Numbers.TryParse(value, out float spacing) || spacing < DrillKit.MinimumSpacing) return $"Spacing must be at least {Numbers.Format(DrillKit.MinimumSpacing)} m.";

            group.Spacing = spacing;
            return string.Empty;
        }

        // Brackets are allowed, so a position can be written like an arena corner.
        private static bool TryParseOffset(string text, out Vector2 offset)
        {
            offset = default;
            string[] parts = text.Trim('(', ')').Split(',');
            if (parts.Length != OffsetParts || !Numbers.TryParse(parts[0], out float x) || !Numbers.TryParse(parts[1], out float z)) return false;

            offset = new Vector2(x, z);
            return true;
        }
    }
}
