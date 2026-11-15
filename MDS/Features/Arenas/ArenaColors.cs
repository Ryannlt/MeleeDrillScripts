using UnityEngine;

namespace MDS.Features.Arenas
{
    // The server names a color in its reply and the client draws it, so both read this one list.
    public static class ArenaColors
    {
        private static readonly (string Name, Color Color)[] Palette =
        {
            ("red", Color.red), ("blue", Color.blue), ("yellow", Color.yellow), ("green", Color.green),
            ("magenta", Color.magenta), ("cyan", Color.cyan), ("white", Color.white),
        };

        public static Color ColorOf(int number) => Palette[IndexOf(number)].Color;

        public static string NameOf(int number) => Palette[IndexOf(number)].Name;

        // Arena numbers have no limit, so the colors repeat.
        private static int IndexOf(int number) => (number - ArenaStore.First) % Palette.Length;
    }
}
