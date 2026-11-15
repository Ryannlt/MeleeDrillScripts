using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    public sealed class StrategySetting : DrillSetting<PickStrategy>
    {
        private readonly List<PickStrategy> _allowed;

        public StrategySetting(string key, IEnumerable<PickStrategy> allowed, Replies replies)
            : this(key, new List<PickStrategy>(allowed), replies)
        {
        }

        private StrategySetting(string key, List<PickStrategy> allowed, Replies replies)
            : base(key, allowed[0], replies)
        {
            _allowed = allowed;
        }

        protected override string Accepted => string.Join("|", _allowed);

        protected override bool TryRead(string text, out PickStrategy value)
        {
            return EnumParser.TryParseStrict(text, out value) && _allowed.Contains(value);
        }
    }
}
