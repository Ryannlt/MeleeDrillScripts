using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    public sealed class StrategySetting : DrillSetting<PickStrategy>
    {
        private readonly List<PickStrategy> _allowed;

        public StrategySetting(string key, Selection selection, Replies replies)
            : base(key, selection.Default, replies)
        {
            _allowed = new List<PickStrategy>(selection.Allowed);
        }

        protected override string Accepted => string.Join("|", _allowed);

        protected override bool TryRead(string text, out PickStrategy value)
        {
            return EnumParser.TryParseStrict(text, out value) && _allowed.Contains(value);
        }
    }
}
