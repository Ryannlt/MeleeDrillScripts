using System;
using MDS.Features.Drills.Spawning;
using MDS.Shared;
using MDS.Shared.Game;

namespace MDS.Features.Drills.Settings
{
    public sealed class SpawnStrategySetting : DrillSetting<SpawnStrategy>
    {
        public SpawnStrategySetting(string key, Replies replies)
            : base(key, SpawnStrategy.Scatter, replies)
        {
        }

        protected override string Accepted => string.Join("|", Enum.GetNames(typeof(SpawnStrategy)));

        protected override bool TryRead(string text, out SpawnStrategy value) => EnumParser.TryParseStrict(text, out value);
    }
}
