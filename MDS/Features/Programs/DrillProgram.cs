using System;
using System.Collections.Generic;
using System.Globalization;
using MDS.Features.Drills;
using MDS.Features.Drills.Picking;

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

    // One drill line of a step: the drill, its rc options as written, and the arenas it runs in.
    public sealed class ProgramDrill
    {
        private readonly string[] _options;

        // The parts as written, the drill's name first.
        public ProgramDrill(IDrill drill, string[] parts, List<int> arenas)
        {
            Drill = drill;
            _options = Array.FindAll(parts[1..], part => !ProgramLineParser.IsArenas(part));
            Arenas = arenas;
            Text = string.Join(",", parts);
        }

        public IDrill Drill { get; }

        public List<int> Arenas { get; }

        public string Text { get; }

        // Fresh options for each fight, so nothing set for one arena carries to the next.
        public DrillOptions OptionsFor(int arena, PickStrategy pick)
        {
            Drill.Syntax.TryParse(_options, out DrillOptions options, out _);
            options.Set(DrillOption.Arena, arena.ToString(CultureInfo.InvariantCulture));
            options.Set(DrillOption.Strategy, pick.ToString());
            return options;
        }
    }

    // How a step picks, deals its arenas, runs and ends, and the drill lines that run in it at once.
    public sealed class ProgramStep
    {
        public PickStrategy Pick { get; set; } = PickStrategy.Random;

        public ArenaStrategy Arenas { get; set; } = ArenaStrategy.Fill;

        public StepMode Mode { get; set; } = StepMode.Together;

        // 0 for no limit.
        public float Minutes { get; set; }

        // 0 for no limit.
        public int Fights { get; set; }

        // Null takes the programFightSeconds setting.
        public float? FightSeconds { get; set; }

        // Null takes the programPause setting.
        public float? PauseSeconds { get; set; }

        public string Text { get; set; } = "step";

        public List<ProgramDrill> Drills { get; } = new();
    }

    public sealed class DrillProgram
    {
        public DrillProgram(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public List<ProgramStep> Steps { get; } = new();

        public string Summary => $"{Name} ({Steps.Count} step{(Steps.Count == 1 ? string.Empty : "s")})";

        public void AddStep(ProgramStep step) => Steps.Add(step);

        // A drill line before any step line opens a step with the defaults.
        public void AddDrill(ProgramDrill drill)
        {
            if (Steps.Count == 0) Steps.Add(new ProgramStep());
            Steps[Steps.Count - 1].Drills.Add(drill);
        }

        // The program as lines the config or rc program add would take back.
        public List<string> Lines()
        {
            var lines = new List<string>();
            foreach (ProgramStep step in Steps)
            {
                lines.Add(step.Text);
                lines.AddRange(step.Drills.ConvertAll(drill => drill.Text));
            }
            return lines;
        }
    }
}
