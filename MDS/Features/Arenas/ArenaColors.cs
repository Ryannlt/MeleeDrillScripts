using System;
using UnityEngine;

namespace MDS.Features.Arenas
{
    // The server names a color in its reply and the client draws it, so both read this one list.
    // Arena numbers have no limit, so past the seven base colors each lap is a shade of them: odd laps darker, even laps paler, with the hue nudged a little.
    // Each shade closes on its floor without reaching it, so no two laps of one color match, and the nudge stays too small to reach the next color.
    // The HBox server strips Color.HSVToRGB and Color.RGBToHSV, so the colors are kept as hue and saturation and converted here.
    public static class ArenaColors
    {
        // Hues are in sixths of the color wheel, so each base color lands exactly on one corner of the RGB cube.
        private const float Sixths = 6f;
        private const int Red = 0;
        private const int Yellow = 1;
        private const int Green = 2;
        private const int Cyan = 3;
        private const int Blue = 4;
        private const int Magenta = 5;
        private const float HueStep = 0.15f;
        private const int HueSteps = 3;
        private const float LowestValue = 0.35f;
        private const float LowestSaturation = 0.15f;
        private const float ShadeLaps = 2f;

        // Laps alternate darker and paler, so each depth spans two laps.
        private const int LapsPerDepth = 2;

        private static readonly (string Name, float Hue, float Saturation)[] Palette =
        {
            ("red", Red, 1f), ("blue", Blue, 1f), ("yellow", Yellow, 1f), ("green", Green, 1f),
            ("magenta", Magenta, 1f), ("cyan", Cyan, 1f), ("white", Red, 0f),
        };

        public static Color ColorOf(int number)
        {
            int lap = LapOf(number);
            (_, float hue, float saturation) = Palette[IndexOf(number)];
            if (lap == 0) return FromHsv(hue, saturation, 1f);
            if (saturation <= 0f) return FromHsv(0f, 0f, Shade(LowestValue, lap));

            int depth = (lap + 1) / LapsPerDepth;
            bool darker = lap % LapsPerDepth == 1;
            float nudged = hue + HueStep * (lap % HueSteps - 1);
            return FromHsv(nudged, darker ? saturation : saturation * Shade(LowestSaturation, depth), darker ? Shade(LowestValue, depth) : 1f);
        }

        // "red", then "red 2", "red 3" for its shades.
        public static string NameOf(int number)
        {
            int lap = LapOf(number);
            string name = Palette[IndexOf(number)].Name;
            return lap == 0 ? name : $"{name} {lap + 1}";
        }

        // 1 at depth 0, falling toward the floor with each step.
        private static float Shade(float floor, int depth) => floor + (1f - floor) * ShadeLaps / (depth + ShadeLaps);

        // The standard conversion, with the hue in sixths.
        private static Color FromHsv(float hue, float saturation, float value)
        {
            float wrapped = hue - Sixths * (float)Math.Floor(hue / Sixths);
            int corner = (int)Math.Floor(wrapped);
            float along = wrapped - corner;
            float low = value * (1f - saturation);
            float falling = value * (1f - saturation * along);
            float rising = value * (1f - saturation * (1f - along));
            switch (corner)
            {
                case Red: return new Color(value, rising, low);
                case Yellow: return new Color(falling, value, low);
                case Green: return new Color(low, value, rising);
                case Cyan: return new Color(low, falling, value);
                case Blue: return new Color(rising, low, value);
                default: return new Color(value, low, falling);
            }
        }

        private static int IndexOf(int number) => (number - ArenaStore.First) % Palette.Length;

        private static int LapOf(int number) => (number - ArenaStore.First) / Palette.Length;
    }
}
