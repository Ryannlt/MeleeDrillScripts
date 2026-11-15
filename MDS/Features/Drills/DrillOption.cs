using MDS.Features.Arenas;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Drills
{
    public enum DrillOption
    {
        Attackers,
        Defenders,
        Strategy,
        Distance,
        Spacing,
        Facing,
        Offset,
        Arena
    }

    public static class DrillOptionValues
    {
        public static bool Accepts(DrillOption option, string text)
        {
            switch (option)
            {
                case DrillOption.Attackers:
                case DrillOption.Defenders:
                    return int.TryParse(text, out int count) && count > 0;
                case DrillOption.Arena:
                    return ArenaParser.TryParseNumber(text, out _);
                case DrillOption.Strategy:
                    return EnumParser.TryParseStrict(text, out PickStrategy _);
                case DrillOption.Facing:
                    return Facing.TryParse(text, out _);
                case DrillOption.Offset:
                    // Zero uses the arena edge to edge, as the old offset setting allowed.
                    return Numbers.TryParseZeroOrMore(text, out _);
                default:
                    return Numbers.TryParsePositive(text, out _);
            }
        }

        // A bare number already means a count, a distance or a spacing, so the arena is only ever given by name.
        public static bool IsNamedOnly(DrillOption option) => option == DrillOption.Arena;

        public static string KeyOf(DrillOption option) => option.ToString().ToLowerInvariant();
    }
}
