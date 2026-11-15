using System.Globalization;

namespace MDS.Shared
{
    // Invariant, so "2.5" reads as 2.5 on a server that writes decimals with a comma.
    public static class Numbers
    {
        public static bool TryParse(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParsePositive(string text, out float value) => TryParse(text, out value) && value > 0f;

        public static bool TryParseZeroOrMore(string text, out float value) => TryParse(text, out value) && value >= 0f;

        // For people: at most two decimals, none when whole.
        public static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        public static string Coordinate(float value) => value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
