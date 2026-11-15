using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Drills
{
    // The options one drill command was given, already checked by DrillSyntax. Anything not given falls back to the
    // drill's configured default.
    public sealed class DrillOptions
    {
        private readonly Dictionary<DrillOption, string> _values = new();

        public bool Has(DrillOption option) => _values.ContainsKey(option);

        public void Set(DrillOption option, string text) => _values[option] = text;

        public int GetCount(DrillOption option) => _values.TryGetValue(option, out string text) ? int.Parse(text) : 0;

        public float GetNumber(DrillOption option, float fallback)
        {
            return _values.TryGetValue(option, out string text) && Numbers.TryParse(text, out float value) ? value : fallback;
        }

        public PickStrategy GetStrategy(PickStrategy fallback)
        {
            return _values.TryGetValue(DrillOption.Strategy, out string text) && EnumParser.TryParseStrict(text, out PickStrategy value) ? value : fallback;
        }

        public Facing GetFacing(Facing fallback)
        {
            return _values.TryGetValue(DrillOption.Facing, out string text) && Facing.TryParse(text, out Facing value) ? value : fallback;
        }
    }
}
