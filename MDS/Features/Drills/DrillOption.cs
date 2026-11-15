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
        Offset
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

        public static string KeyOf(DrillOption option) => option.ToString().ToLowerInvariant();
    }
}
