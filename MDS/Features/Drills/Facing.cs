using System;
using System.Collections.Generic;
using MDS.Shared;

namespace MDS.Features.Drills
{
    public readonly struct Facing
    {
        private const string RandomName = "Random";

        // What the old orientation setting took for random, still read so existing configs keep working.
        private const string OldRandom = "-1";
        private const float FullTurn = 360f;
        private const float WestEastDegrees = 0f;
        private const float NorthSouthDegrees = 90f;
        private const float EastWestDegrees = 180f;
        private const float SouthNorthDegrees = 270f;

        private static readonly Dictionary<string, float> Named = new(StringComparer.OrdinalIgnoreCase)
        {
            { "WestEast", WestEastDegrees },
            { "NorthSouth", NorthSouthDegrees },
            { "EastWest", EastWestDegrees },
            { "SouthNorth", SouthNorthDegrees },
        };

        private Facing(float degrees, bool isRandom)
        {
            Degrees = degrees;
            IsRandom = isRandom;
        }

        public static Facing NorthSouth => new Facing(NorthSouthDegrees, false);

        public static Facing WestEast => new Facing(WestEastDegrees, false);

        public static Facing Random => new Facing(0f, true);

        public float Degrees { get; }

        public bool IsRandom { get; }

        public static bool TryParse(string text, out Facing facing)
        {
            facing = default;
            if (string.Equals(text, RandomName, StringComparison.OrdinalIgnoreCase) || text == OldRandom) facing = Random;
            else if (Named.TryGetValue(text ?? string.Empty, out float named)) facing = new Facing(named, false);
            else if (Numbers.TryParse(text, out float degrees) && degrees >= 0f && degrees < FullTurn) facing = new Facing(degrees, false);
            else return false;

            return true;
        }

        public float Resolve(System.Random random) => IsRandom ? (float)(random.NextDouble() * FullTurn) : Degrees;

        public override string ToString()
        {
            if (IsRandom) return RandomName;

            foreach (var pair in Named)
            {
                if (pair.Value == Degrees) return $"{pair.Key} ({Numbers.Format(Degrees)} degrees)";
            }
            return $"{Numbers.Format(Degrees)} degrees";
        }
    }
}
