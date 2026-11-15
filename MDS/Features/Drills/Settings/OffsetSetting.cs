using MDS.Shared;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    // Apart from NumberSetting because zero is allowed: an offset of 0 uses the arena edge to edge.
    public sealed class OffsetSetting : DrillSetting<float>
    {
        public OffsetSetting(string key, float value, Replies replies)
            : base(key, value, replies)
        {
        }

        protected override string Accepted => "meters, 0 or more";

        protected override bool TryRead(string text, out float value) => Numbers.TryParseZeroOrMore(text, out value);

        protected override string Show(float value) => Numbers.Format(value);
    }
}
