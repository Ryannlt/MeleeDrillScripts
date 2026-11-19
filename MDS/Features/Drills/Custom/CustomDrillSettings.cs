using System;
using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Drills.Custom
{
    // rc customdrill <name> set takes its settings in order: [strategy] [turn] [countdown on|off] [godmode on|off] [godModeSeconds].
    // A word goes to the first open slot that takes it, "-" keeps a slot as it is, and key=value names one.
    // Each filled slot becomes the config's own setting line, so the line parser checks and applies it.
    public static class CustomDrillSettings
    {
        public const string Format = "[random|any|next|repeat] [turn] [countdown on|off] [godmode on|off] [godModeSeconds]";

        private static readonly (string Key, Func<string, bool> Takes)[] Slots =
        {
            ("strategy", word => EnumParser.TryParseStrict(word, out PickStrategy _)),
            ("turn", word => word.Equals("random", StringComparison.OrdinalIgnoreCase) || Numbers.TryParse(word, out float _)),
            ("countdown", IsSwitch),
            ("godmode", IsSwitch),
            ("godModeSeconds", word => Numbers.TryParsePositive(word, out float _)),
        };

        public static string Read(string[] words, out List<string> lines)
        {
            lines = new List<string>();
            var filled = new bool[Slots.Length];
            foreach (string word in words)
            {
                string line = LineFor(word, filled);
                if (line == null) return $"'{word}' isn't a custom drill setting. They read: {Format}";
                if (line.Length > 0) lines.Add(line);
            }
            return string.Empty;
        }

        // Null when nothing takes the word, empty for a "-".
        private static string LineFor(string word, bool[] filled)
        {
            int split = word.IndexOf('=');
            if (split > 0) return Named(word.Substring(0, split), word.Substring(split + 1), filled);

            for (int slot = 0; slot < Slots.Length; slot++)
            {
                if (filled[slot] || (word != DrillSyntax.Skip && !Slots[slot].Takes(word))) continue;

                filled[slot] = true;
                return word == DrillSyntax.Skip ? string.Empty : $"{Slots[slot].Key},{word}";
            }
            return null;
        }

        private static string Named(string key, string value, bool[] filled)
        {
            int slot = Array.FindIndex(Slots, s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (slot < 0) return null;

            filled[slot] = true;
            return $"{Slots[slot].Key},{value}";
        }

        private static bool IsSwitch(string word) => word.Equals("on", StringComparison.OrdinalIgnoreCase) || word.Equals("off", StringComparison.OrdinalIgnoreCase);
    }
}
