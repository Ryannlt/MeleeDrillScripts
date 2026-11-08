using System;

namespace MDS.Core
{
    public static class EnumParser
    {
        public static bool TryParseEnumStrict<T>(string input, out T result) where T : struct, Enum
        {
            result = default;

            // Reject numeric input to avoid unintended parsing by underlying int values
            if (int.TryParse(input, out _))
            {
                return false;
            }

            // Attempt normal case-insensitive parse on string name
            return Enum.TryParse(input, ignoreCase: true, out result);
        }
    }
}
