using System.Collections.Generic;
using MDS.Features.Arenas;
using MDS.Shared;
using UnityEngine;

namespace MDS.Features.Drills
{
    // Two facing lines shrunk to stand inside their arena: distance gives way first, then spacing, each to the largest value that fits.
    // Each line is a box of half-length distance / 2 along the facing and spacing x (count - 1) / 2 across it, turned by the facing.
    public sealed class LineFit
    {
        private const float Half = 0.5f;

        // Room for float rounding at the arena's edge.
        private const float Slack = 0.001f;

        private LineFit(LineShape shape, string note, string error)
        {
            Shape = shape;
            Note = note;
            Error = error;
        }

        public LineShape Shape { get; }

        // What shrank, for whoever started the drill, and empty when nothing did.
        public string Note { get; }

        // Why the lines don't fit even at the smallest distance and spacing, and empty when they do.
        public string Error { get; }

        public bool Fits => Error.Length == 0;

        public static LineFit Into(Arena arena, LineShape asked, int longestLine)
        {
            var room = new Room(arena.Bounds, asked);
            float across = (longestLine - 1) * Half;
            float halfDistance = room.HalfDistanceFor(asked.Spacing * across);
            if (halfDistance + Slack >= asked.Distance * Half) return new LineFit(asked, string.Empty, string.Empty);

            float spacing = asked.Spacing;
            float smallestHalfDistance = Mathf.Min(DrillKit.MinimumSpacing, asked.Distance) * Half;
            if (halfDistance < smallestHalfDistance && across > 0f)
            {
                spacing = Numbers.RoundDown(Mathf.Min(asked.Spacing, room.HalfLineFor(smallestHalfDistance) / across));
                halfDistance = room.HalfDistanceFor(spacing * across);
            }

            LineShape fitted = asked.Resized(Numbers.RoundDown(Mathf.Min(asked.Distance, halfDistance + halfDistance)), spacing);
            if (!IsBigEnough(asked, fitted)) return new LineFit(asked, string.Empty, $"Arena {arena.Number} is too small for lines of {longestLine}, even at {Numbers.Format(DrillKit.MinimumSpacing)} m distance and spacing.");
            return new LineFit(fitted, NoteFor(arena, asked, fitted), string.Empty);
        }

        private static bool IsBigEnough(LineShape asked, LineShape fitted)
        {
            return fitted.Distance + Slack >= Mathf.Min(DrillKit.MinimumSpacing, asked.Distance) && fitted.Spacing + Slack >= Mathf.Min(DrillKit.MinimumSpacing, asked.Spacing);
        }

        private static string NoteFor(Arena arena, LineShape asked, LineShape fitted)
        {
            var changes = new List<string>();
            if (fitted.Distance < asked.Distance) changes.Add(FitNote.Change("distance", asked.Distance, fitted.Distance));
            if (fitted.Spacing < asked.Spacing) changes.Add(FitNote.Change("spacing", asked.Spacing, fitted.Spacing));
            return FitNote.Of(arena.Number, changes);
        }

        // How far the box may reach from the lines' center along each axis of the arena.
        private sealed class Room
        {
            // Below this a facing runs straight along an axis, and that axis puts no limit on the turned side.
            private const float Straight = 0.0001f;

            private readonly float _cos;
            private readonly float _sin;
            private readonly float _x;
            private readonly float _z;

            public Room(Rect area, LineShape shape)
            {
                float radians = shape.Degrees * Mathf.Deg2Rad;
                _cos = Mathf.Abs(Mathf.Cos(radians));
                _sin = Mathf.Abs(Mathf.Sin(radians));
                _x = Mathf.Min(shape.Center.x - area.xMin, area.xMax - shape.Center.x);
                _z = Mathf.Min(shape.Center.y - area.yMin, area.yMax - shape.Center.y);
            }

            public float HalfDistanceFor(float halfLine) => Mathf.Min(Limit(_x - halfLine * _sin, _cos), Limit(_z - halfLine * _cos, _sin));

            public float HalfLineFor(float halfDistance) => Mathf.Min(Limit(_x - halfDistance * _cos, _sin), Limit(_z - halfDistance * _sin, _cos));

            private static float Limit(float left, float share)
            {
                if (share >= Straight) return left / share;
                return left + Slack >= 0f ? float.PositiveInfinity : float.NegativeInfinity;
            }
        }
    }
}
