using System;
using System.Collections.Generic;
using System.Text;

namespace MDS.Features.Arenas
{
    // Each arena in its config form, "2:(x,z),(x,z)", so the client reads it with the config parser.
    public static class ArenaViewMessage
    {
        public const string Name = "MDS.ArenaView";

        // Sent instead of an empty payload when there is nothing to draw.
        private const string Nothing = "none";
        private const char Separator = ';';

        public static byte[] Encode(IEnumerable<Arena> arenas)
        {
            var parts = new List<string>();
            foreach (Arena arena in arenas) parts.Add($"{arena.Number}:{Arena.Format(arena.Corner1)},{Arena.Format(arena.Corner2)}");
            return Encoding.UTF8.GetBytes(parts.Count == 0 ? Nothing : string.Join(Separator.ToString(), parts));
        }

        public static byte[] EncodeNothing() => Encode(Array.Empty<Arena>());

        // A malformed part is skipped rather than failing the rest.
        public static List<Arena> Decode(byte[] data)
        {
            var arenas = new List<Arena>();
            foreach (string part in Encoding.UTF8.GetString(data ?? Array.Empty<byte>()).Split(Separator))
            {
                if (ArenaParser.TryParseArena(part, out Arena arena)) arenas.Add(arena);
            }
            return arenas;
        }
    }
}
