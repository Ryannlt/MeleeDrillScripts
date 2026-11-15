using System.Globalization;

namespace MDS.Shared
{
    // Numbers in commands, config lines and console commands, read and written the same whatever the server's
    // language: "2.5" stays 2.5 on a server that writes decimals with a comma.
    public static class Numbers
    {
        public static bool TryParse(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParsePositive(string text, out float value) => TryParse(text, out value) && value > 0f;

        // For people: at most two decimals, none when whole.
        public static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        // For console commands: always two decimals.
        public static string Coordinate(float value) => value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
