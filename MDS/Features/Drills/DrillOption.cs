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
        // Whether the text is a value this option can take. Counts are whole and above zero, distances above zero.
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
                default:
                    return Numbers.TryParsePositive(text, out _);
            }
        }

        // The key used in key=value: the option's name in lower case.
        public static string KeyOf(DrillOption option) => option.ToString().ToLowerInvariant();
    }
}
