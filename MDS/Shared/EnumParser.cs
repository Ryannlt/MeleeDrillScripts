using System;

namespace MDS.Shared
{
    public static class EnumParser
    {
        // Names only, ignoring case. Numbers are refused, so "3" can't silently pick the fourth value.
        public static bool TryParseStrict<T>(string input, out T result) where T : struct, Enum
        {
            result = default;
            return !int.TryParse(input, out _) && Enum.TryParse(input, true, out result);
        }
    }
}
