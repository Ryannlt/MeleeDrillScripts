using System.Collections.Generic;
using MDS.Features.Drills.Picking;
using MDS.Shared.Config;
using MDS.Shared.Game;
using MDS.Shared.Settings;

namespace MDS.Features.Drills.Settings
{
    // The drill defaults, each one an rc set/get key and a config key of the same name. The defaults are tuned for
    // Palisade Arena A1.
    public sealed class DrillSettings
    {
        private const float XvXDistanceDefault = 20f;
        private const float GroupfightDistanceDefault = 25f;
        private const float LineSpacingDefault = 2f;
        private const float OpenMeleeSpacingDefault = 1.5f;
        private const float OpenMeleeOffsetDefault = 2f;

        public DrillSettings(Replies replies, Selection xvx, Selection groupfight)
        {
            XvXDistance = new NumberSetting("xvxDistance", XvXDistanceDefault, replies);
            XvXSpacing = new NumberSetting("xvxSpacing", LineSpacingDefault, replies);
            XvXStrategy = new StrategySetting("xvxStrategy", xvx.Allowed, replies);
            GroupfightDistance = new NumberSetting("groupfightDistance", GroupfightDistanceDefault, replies);
            GroupfightSpacing = new NumberSetting("groupfightSpacing", LineSpacingDefault, replies);
            GroupfightStrategy = new StrategySetting("groupfightStrategy", groupfight.Allowed, replies);
            OpenMeleeSpacing = new NumberSetting("openMeleeSpacing", OpenMeleeSpacingDefault, replies);
            OpenMeleeOffset = new NumberSetting("openMeleeOffset", OpenMeleeOffsetDefault, replies);
            Orientation = new FacingSetting(replies);
        }

        public NumberSetting XvXDistance { get; }

        public NumberSetting XvXSpacing { get; }

        public StrategySetting XvXStrategy { get; }

        public NumberSetting GroupfightDistance { get; }

        public NumberSetting GroupfightSpacing { get; }

        public StrategySetting GroupfightStrategy { get; }

        public NumberSetting OpenMeleeSpacing { get; }

        public NumberSetting OpenMeleeOffset { get; }

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
                OpenMeleeSpacing, OpenMeleeOffset, Orientation,
            };
        }
    }
}
