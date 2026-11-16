using MDS.Features.Drills;
using MDS.Features.Drills.Settings;
using MDS.Shared;
using MDS.Shared.Config;
using MDS.Shared.Game;
using MDS.Shared.Settings;

namespace MDS.Features.Programs
{
    // What a running program uses: the drills, the fight watcher, its settings, and the console for its broadcasts.
    public sealed class ProgramKit
    {
        public DrillCatalog Drills { get; set; }

        public FightWatcher Watcher { get; set; }

        public ProgramSettings Settings { get; set; }

        public GameConsole Console { get; set; }

        public Replies Replies { get; set; }

        public IClock Clock { get; set; }
    }

    public sealed class SecondsSetting : DrillSetting<float>
    {
        public SecondsSetting(string key, float value, Replies replies)
            : base(key, value, replies)
        {
        }

        protected override string Accepted => "seconds";

        protected override bool TryRead(string text, out float value) => Numbers.TryParseZeroOrMore(text, out value);

        protected override string Show(float value) => Numbers.Format(value);
    }

    public sealed class ProgramSettings
    {
        private const float PauseDefault = 5f;
        private const float FightDefault = 90f;

        public ProgramSettings(Replies replies)
        {
            Pause = new SecondsSetting("programPause", PauseDefault, replies);
            FightSeconds = new SecondsSetting("programFightSeconds", FightDefault, replies);
        }

        // Before each step, with a broadcast.
        public SecondsSetting Pause { get; }

        // The longest a fight runs before the program calls it over.
        public SecondsSetting FightSeconds { get; }

        public void RegisterWith(SettingsRegistry settings, ConfigRouter config)
        {
            foreach (SecondsSetting setting in new[] { Pause, FightSeconds })
            {
                settings.Register(setting);
                config.Register(setting);
            }
        }
    }
}
