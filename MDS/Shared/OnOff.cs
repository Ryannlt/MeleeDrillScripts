using System;
using System.Collections.Generic;

namespace MDS.Shared
{
    public static class OnOff
    {
        private static readonly Dictionary<string, bool> States = new(StringComparer.OrdinalIgnoreCase)
        {
            { "on", true }, { "true", true }, { "off", false }, { "false", false },
        };

        public static bool TryParse(string text, out bool on) => States.TryGetValue(text ?? string.Empty, out on);
    }
}
