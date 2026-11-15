using System.Collections.Generic;
using UnityEngine;

namespace MDS.Features.Arenas
{
    // Digits as straight strokes, so the client draws them with the same mesh as the border and needs no font.
    // A digit fills a cell one wide and two tall, with its origin at the bottom left.
    public static class SevenSegment
    {
        public const float CellWidth = 1f;
        public const float CellHeight = 2f;

        private const float Left = 0f;
        private const float Right = CellWidth;
        private const float Bottom = 0f;
        private const float Middle = CellHeight / 2f;
        private const float Top = CellHeight;

        // Segments a to g, clockwise from the top, then the middle bar.
        private static readonly (Vector2 From, Vector2 To)[] Segments =
        {
            (new Vector2(Left, Top), new Vector2(Right, Top)),
            (new Vector2(Right, Top), new Vector2(Right, Middle)),
            (new Vector2(Right, Middle), new Vector2(Right, Bottom)),
            (new Vector2(Left, Bottom), new Vector2(Right, Bottom)),
            (new Vector2(Left, Bottom), new Vector2(Left, Middle)),
            (new Vector2(Left, Middle), new Vector2(Left, Top)),
            (new Vector2(Left, Middle), new Vector2(Right, Middle)),
        };

        private static readonly string[] LitSegments = { "abcdef", "bc", "abdeg", "abcdg", "bcfg", "acdfg", "acdefg", "abc", "abcdefg", "abcdfg" };

        public static List<(Vector2 From, Vector2 To)> StrokesOf(char digit)
        {
            var strokes = new List<(Vector2 From, Vector2 To)>();
            if (digit < '0' || digit > '9') return strokes;

            foreach (char segment in LitSegments[digit - '0']) strokes.Add(Segments[segment - 'a']);
            return strokes;
        }
    }
}
