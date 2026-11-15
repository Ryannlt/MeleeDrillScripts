using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Features.Drills.Popcorn;
using MDS.Shared.Config;
using MDS.Shared.Game;
using MDS.Shared.Settings;

namespace MDS.Features.Drills.Settings
{
    // The defaults are tuned for Palisade Arena A1.
    public sealed class DrillSettings
    {
        private const float XvXDistanceDefault = 20f;
        private const float GroupfightDistanceDefault = 25f;
        private const float LineSpacingDefault = 2f;
        private const float OpenMeleeSpacingDefault = 1.5f;
        private const float OpenMeleeOffsetDefault = 2f;
        private const float PopcornDistanceDefault = 6f;
        private const float PopcornLineDistanceDefault = 6f;
        private static readonly FightSize PopcornMinDefault = new FightSize(2, 1);
        private static readonly FightSize PopcornMaxDefault = new FightSize(4, 4);

        public DrillSettings(Replies replies, Selection xvx, Selection groupfight)
        {
            XvXDistance = new NumberSetting("xvxDistance", XvXDistanceDefault, replies);
            XvXSpacing = new NumberSetting("xvxSpacing", LineSpacingDefault, replies);
            XvXStrategy = new StrategySetting("xvxStrategy", xvx.Allowed, replies);
            GroupfightDistance = new NumberSetting("groupfightDistance", GroupfightDistanceDefault, replies);
            GroupfightSpacing = new NumberSetting("groupfightSpacing", LineSpacingDefault, replies);
            GroupfightStrategy = new StrategySetting("groupfightStrategy", groupfight.Allowed, replies);
            OpenMeleeSpacing = new NumberSetting("openMeleeSpacing", OpenMeleeSpacingDefault, replies);
            OpenMeleeOffset = new OffsetSetting("openMeleeOffset", OpenMeleeOffsetDefault, replies);
            OpenMeleeStrategy = new SpawnStrategySetting("openMeleeStrategy", replies);
            PopcornMin = new FightSizeSetting("popcornMin", PopcornMinDefault, replies);
            PopcornMax = new FightSizeSetting("popcornMax", PopcornMaxDefault, replies);
            PopcornDistance = new NumberSetting("popcornDistance", PopcornDistanceDefault, replies);
            PopcornLineDistance = new NumberSetting("popcornLineDistance", PopcornLineDistanceDefault, replies);
            PopcornSpacing = new NumberSetting("popcornSpacing", LineSpacingDefault, replies);
            PopcornStrategy = new FightLayoutSetting("popcornStrategy", replies);
            Orientation = new FacingSetting(replies);
        }

        public NumberSetting XvXDistance { get; }

        public NumberSetting XvXSpacing { get; }

        public StrategySetting XvXStrategy { get; }

        public NumberSetting GroupfightDistance { get; }

        public NumberSetting GroupfightSpacing { get; }

        public StrategySetting GroupfightStrategy { get; }

        public NumberSetting OpenMeleeSpacing { get; }

        public OffsetSetting OpenMeleeOffset { get; }

        public SpawnStrategySetting OpenMeleeStrategy { get; }

        public FightSizeSetting PopcornMin { get; }

        public FightSizeSetting PopcornMax { get; }

        public NumberSetting PopcornDistance { get; }

        public NumberSetting PopcornLineDistance { get; }

        public NumberSetting PopcornSpacing { get; }

        public FightLayoutSetting PopcornStrategy { get; }

        public FacingSetting Orientation { get; }

        public void RegisterWith(SettingsRegistry settings, ConfigRouter config)
        {
            foreach (ISetting setting in All())
            {
                settings.Register(setting);
                config.Register((IConfigVariable)setting);
            }
        }

        private IEnumerable<ISetting> All()
        {
            return new ISetting[]
            {
                XvXDistance, XvXSpacing, XvXStrategy, GroupfightDistance, GroupfightSpacing, GroupfightStrategy,
                OpenMeleeSpacing, OpenMeleeOffset, OpenMeleeStrategy, PopcornMin, PopcornMax, PopcornDistance, PopcornLineDistance, PopcornSpacing,
                PopcornStrategy, Orientation,
            };
        }
    }
}
