using System;
using MDS.Features.Drills.Popcorn;
using MDS.Shared;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    public sealed class FightLayoutSetting : DrillSetting<FightLayout>
    {
        public FightLayoutSetting(string key, Replies replies)
            : base(key, FightLayout.Spread, replies)
        {
        }

        protected override string Accepted => string.Join("|", Enum.GetNames(typeof(FightLayout)));

        protected override bool TryRead(string text, out FightLayout value) => EnumParser.TryParseStrict(text, out value);
    }
}
