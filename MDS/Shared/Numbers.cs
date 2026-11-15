using System.Globalization;
using UnityEngine;

namespace MDS.Shared
{
    // Invariant, so "2.5" reads as 2.5 on a server that writes decimals with a comma.
    public static class Numbers
    {
        private const float Hundredths = 100f;

        // Keeps float error from turning 0.5 into 0.49.
        private const float RoundingSlack = 0.001f;

        public static bool TryParse(string text, out float value)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParsePositive(string text, out float value) => TryParse(text, out value) && value > 0f;

        public static bool TryParseZeroOrMore(string text, out float value) => TryParse(text, out value) && value >= 0f;

        // For people: at most two decimals, none when whole.
        public static string Format(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        // To the hundredth below, so a shrunk value shows as the value used and still fits.
        public static float RoundDown(float value) => Mathf.Floor(value * Hundredths + RoundingSlack) / Hundredths;

        public static string Coordinate(float value) => value.ToString("F2", CultureInfo.InvariantCulture);
    }
}
