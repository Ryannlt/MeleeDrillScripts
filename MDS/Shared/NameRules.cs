using System;
using System.Collections.Generic;

namespace MDS.Shared
{
    // Names people type in rc and in the config, for custom drills and programs.
    public static class NameRules
    {
        private const int Longest = 32;

        // Letters, digits, - and _: not a bare number, which picks by place in a list, and not a word the command already uses.
        public static bool TryCheck(string name, IReadOnlyCollection<string> reserved, out string error)
        {
            bool shaped = !string.IsNullOrEmpty(name) && name.Length <= Longest && Array.TrueForAll(name.ToCharArray(), c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
            if (!shaped) error = $"'{name}' isn't a name: use up to {Longest} letters, digits, - and _.";
            else if (int.TryParse(name, out _)) error = $"'{name}' would read as a number: give it a name with a letter in it.";
            else if (new List<string>(reserved).Exists(word => word.Equals(name, StringComparison.OrdinalIgnoreCase))) error = $"'{name}' is a word the command already uses: pick another name.";
            else error = string.Empty;
            return error.Length == 0;
        }

        // A config value of the form <name>:<line>, where the line keeps any further colons.
        public static bool TrySplit(string value, out string name, out string line)
        {
            int colon = (value ?? string.Empty).IndexOf(':');
            name = colon > 0 ? value.Substring(0, colon) : string.Empty;
            line = colon > 0 ? value.Substring(colon + 1) : string.Empty;
            return colon > 0;
        }
    }
}
