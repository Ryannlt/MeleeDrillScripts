using System;

namespace MDS.Features.Drills.Popcorn
{
    // A fight size written as "2v1": only the total counts, since popcorn splits every fight as evenly as it can.
    public readonly struct FightSize
    {
        private const char Versus = 'v';
        private const int Sides = 2;

        // More than any server holds, and small enough that a total can't overflow.
        private const int MostPerSide = 100;

        public FightSize(int first, int second)
        {
            First = first;
            Second = second;
        }

        public int First { get; }

        public int Second { get; }

        public int Total => First + Second;

        public static bool TryParse(string text, out FightSize size)
        {
            size = default;
            string[] sides = (text ?? string.Empty).ToLowerInvariant().Split(Versus);
            if (sides.Length != Sides || !int.TryParse(sides[0], out int first) || !int.TryParse(sides[1], out int second) || first < 1 || second < 1 || first > MostPerSide || second > MostPerSide) return false;

            size = new FightSize(first, second);
            return true;
        }

        public override string ToString() => $"{First}{Versus}{Second}";
    }
}
