using MDS.Shared;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    // A distance in meters, above zero.
    public sealed class NumberSetting : DrillSetting<float>
    {
        public NumberSetting(string key, float value, Replies replies)
            : base(key, value, replies)
        {
        }

        protected override string Accepted => "meters";

        protected override bool TryRead(string text, out float value) => Numbers.TryParsePositive(text, out value);

        protected override string Show(float value) => Numbers.Format(value);
    }
}
