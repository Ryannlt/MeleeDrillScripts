using System.Collections.Generic;
using MDS.Features.Drills;
using MDS.Shared.Game;

namespace MDS.Features.Programs
{
    // Runs one program at a time.
    public sealed class ProgramRunner
    {
        private readonly ProgramKit _kit;
        private readonly Routines _routines;
        private ProgramRun _current;
        private Routine _routine;

        public ProgramRunner(ProgramKit kit, Routines routines)
        {
            _kit = kit;
            _routines = routines;
        }

        public ProgramRun Current => _current != null && !_current.IsDone ? _current : null;

        // Every drill line is checked in every arena with its step's pick before anything starts, so a program never stops halfway on a typo.
        public string Refusal(DrillProgram program)
        {
            if (program.Steps.Count == 0) return $"{program.Name} has no steps yet.";

            for (int i = 0; i < program.Steps.Count; i++)
            {
                string error = StepRefusal(program.Steps[i]);
                if (error.Length > 0) return $"Step {i + 1} of {program.Name}: {error}";
            }
            return string.Empty;
        }

        public void Start(DrillProgram program, bool loops, int starter)
        {
            _current = new ProgramRun(_kit, program, starter) { Loops = loops };
            _routine = _routines.Start(_current.Body(), $"program {program.Name}");
        }

        public void Stop()
        {
            _current?.Stop();
            _routines.Stop(_routine);
            _current = null;
        }

        private static string DrillRefusal(ProgramDrill line, ProgramStep step)
        {
            foreach (int arena in line.Arenas)
            {
                DrillOptions options = line.OptionsFor(arena, step.Pick);
                string error = ProgramLineParser.ParallelRefusal(line.Drill, options, line.Arenas.Count);
                if (error.Length == 0) line.Drill.Validate(options, out error);
                if (error.Length > 0) return $"{line.Text}: {error}";
            }
            return string.Empty;
        }

        // Two fights in one arena would each clear the other's players away.
        private static string SharedArena(ProgramStep step)
        {
            var used = new HashSet<int>();
            foreach (ProgramDrill line in step.Drills)
            {
                int shared = line.Arenas.Find(arena => !used.Add(arena));
                if (shared > 0) return $"arena {shared} is used by two drill lines.";
            }
            return string.Empty;
        }

        private static string StepRefusal(ProgramStep step)
        {
            if (step.Drills.Count == 0) return "it has no drill lines.";
            if (SharedArena(step).Length > 0) return SharedArena(step);

            foreach (ProgramDrill line in step.Drills)
            {
                string error = DrillRefusal(line, step);
                if (error.Length > 0) return error;
            }
            return string.Empty;
        }
    }
}
