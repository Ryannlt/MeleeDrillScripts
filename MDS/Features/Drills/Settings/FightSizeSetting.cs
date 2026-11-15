using MDS.Features.Drills.Popcorn;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    public sealed class FightSizeSetting : DrillSetting<FightSize>
    {
        public FightSizeSetting(string key, FightSize value, Replies replies)
            : base(key, value, replies)
        {
        }

        protected override string Accepted => "attackers v defenders, such as 2v1";

        protected override bool TryRead(string text, out FightSize value) => FightSize.TryParse(text, out value);
    }
}
