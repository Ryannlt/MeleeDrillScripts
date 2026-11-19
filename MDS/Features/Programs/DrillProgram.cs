using System.Collections.Generic;
using System.Globalization;
using MDS.Features.Drills;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Programs
{
    public enum StepMode
    {
        Together,
        Rolling
    }

    public enum ArenaStrategy
    {
        Fill,
        Balance
    }

    public enum StepEnd
    {
        Round,
        Rounds,
        Each,
        Minutes
    }

    // A drill's own timing in a program; anything left null takes the program's settings.
    public sealed class DrillPace
    {
        // Runs in the step of the drill above, on the players it leaves free, and takes that drill's timing.
        public bool With { get; set; }

        public PickStrategy? Pick { get; set; }

        public StepMode? Deal { get; set; }

        public StepEnd? End { get; set; }

        // Rounds, fights each or minutes, by End.
        public float Count { get; set; }

        public float? Pause { get; set; }

        public float? FightSeconds { get; set; }

        public bool HasTiming => Pick.HasValue || Deal.HasValue || End.HasValue || Pause.HasValue || FightSeconds.HasValue;
    }

    // One drill of a program: the drill, its rc options as written, the arenas it runs in, and its own timing.
    public sealed class ProgramDrill
    {
        public IDrill Drill { get; set; }

        public string[] Options { get; set; } = new string[0];

        public List<int> Arenas { get; set; } = new();

        public DrillPace Pace { get; set; } = new();

        // The words after add, as the config and rc program add take them.
        public string Text { get; set; } = string.Empty;

        // Fresh options for each fight, so nothing set for one arena carries to the next.
        public DrillOptions OptionsFor(int arena, PickStrategy pick)
        {
            Drill.Syntax.TryParse(Options, out DrillOptions options, out _);
            options.Set(DrillOption.Arena, arena.ToString(CultureInfo.InvariantCulture));
            if (Drill.ClearsLeftovers) options.Set(DrillOption.Strategy, pick.ToString());
            return options;
        }
    }

    // What every drill in a program takes unless it sets its own.
    public sealed class ProgramSettings
    {
        public const float PauseDefault = 5f;

        public PickStrategy Pick { get; set; } = PickStrategy.Next;

        public ArenaStrategy Sides { get; set; } = ArenaStrategy.Fill;

        public StepMode Deal { get; set; } = StepMode.Together;

        public float Pause { get; set; } = PauseDefault;

        // 0 is no limit.
        public float FightSeconds { get; set; }

        public bool Loop { get; set; }

        public ProgramSettings Copy() => (ProgramSettings)MemberwiseClone();

        // The settings that differ from the defaults, in the order rc program set takes them.
        public List<string> Words()
        {
            var defaults = new ProgramSettings();
            var words = new List<string>();
            if (Pick != defaults.Pick) words.Add(Lower(Pick));
            if (Sides != defaults.Sides) words.Add(Lower(Sides));
            if (Deal != defaults.Deal) words.Add(Lower(Deal));
            if (Pause != defaults.Pause || FightSeconds != defaults.FightSeconds) words.Add(Numbers.Format(Pause));
            if (FightSeconds != defaults.FightSeconds) words.Add(Numbers.Format(FightSeconds));
            if (Loop) words.Add(ProgramLineParser.LoopWord);
            return words;
        }

        private static string Lower(object value) => value.ToString().ToLowerInvariant();
    }

    // One step as it runs: the drills that start together and how they pick, deal and end, with the program's settings filled in.
    public sealed class ProgramStep
    {
        public PickStrategy Pick { get; set; } = PickStrategy.Next;

        public ArenaStrategy Arenas { get; set; } = ArenaStrategy.Fill;

        public StepMode Mode { get; set; } = StepMode.Together;

        public StepEnd End { get; set; } = StepEnd.Round;

        public float Count { get; set; }

        public float PauseSeconds { get; set; } = ProgramSettings.PauseDefault;

        // 0 is no limit.
        public float FightSeconds { get; set; }

        public List<ProgramDrill> Drills { get; } = new();
    }

    // What a run works through: a snapshot, so editing the program never changes a run under way.
    public sealed class ProgramPlan
    {
        public string Name { get; set; } = string.Empty;

        public List<ProgramStep> Steps { get; set; } = new();

        public bool Loops { get; set; }
    }

    public sealed class DrillProgram
    {
        private const string SetWord = "set";
        private const string AddWord = "add";

        public DrillProgram(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public ProgramSettings Settings { get; set; } = new();

        public List<ProgramDrill> Drills { get; } = new();

        public string Summary => $"{Name} ({Drills.Count} drill{(Drills.Count == 1 ? string.Empty : "s")})";

        public ProgramPlan Plan() => new ProgramPlan { Name = Name, Steps = Steps(), Loops = Settings.Loop };

        // The step holding drill index, run once.
        public ProgramPlan TryPlan(int index)
        {
            List<ProgramStep> steps = Steps();
            ProgramStep step = steps[StepOf(index)];
            step.End = StepEnd.Round;
            step.Mode = StepMode.Together;
            return new ProgramPlan { Name = $"{Name} drill {index + 1}", Steps = new List<ProgramStep> { step } };
        }

        public List<ProgramStep> Steps()
        {
            var steps = new List<ProgramStep>();
            foreach (ProgramDrill drill in Drills)
            {
                if (drill.Pace.With && steps.Count > 0) steps[steps.Count - 1].Drills.Add(drill);
                else steps.Add(StepFor(drill));
            }
            return steps;
        }

        // The program as lines the config or rc program would take back.
        public List<string> Lines()
        {
            var lines = new List<string>();
            List<string> settings = Settings.Words();
            if (settings.Count > 0) lines.Add($"{SetWord},{string.Join(",", settings)}");
            lines.AddRange(Drills.ConvertAll(drill => $"{AddWord},{drill.Text}"));
            return lines;
        }

        private int StepOf(int index)
        {
            int step = -1;
            for (int i = 0; i <= index; i++)
            {
                if (!Drills[i].Pace.With || i == 0) step++;
            }
            return step;
        }

        // An explicit round count runs together; a rolling step with no end runs until everyone has fought once.
        private ProgramStep StepFor(ProgramDrill drill)
        {
            DrillPace pace = drill.Pace;
            bool counted = pace.End == StepEnd.Round || pace.End == StepEnd.Rounds;
            var step = new ProgramStep
            {
                Pick = pace.Pick ?? Settings.Pick, Arenas = Settings.Sides, Mode = pace.Deal ?? (counted ? StepMode.Together : Settings.Deal),
                End = pace.End ?? StepEnd.Round, Count = pace.Count, PauseSeconds = pace.Pause ?? Settings.Pause, FightSeconds = pace.FightSeconds ?? Settings.FightSeconds,
            };
            if (step.Mode == StepMode.Rolling && !pace.End.HasValue) (step.End, step.Count) = (StepEnd.Each, 1f);
            step.Drills.Add(drill);
            return step;
        }
    }
}
