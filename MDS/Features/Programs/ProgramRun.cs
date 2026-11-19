using System.Collections;
using MDS.Features.Drills.Picking;
using MDS.Shared;

namespace MDS.Features.Programs
{
    // One program from start to stop, stepped every tick: a pause before each step, then the step until it is done.
    // Routines run one flat routine, so the phases are states here rather than nested routines.
    public sealed class ProgramRun
    {
        private readonly ProgramKit _kit;
        private readonly ProgramPlan _plan;
        private readonly int _starter;
        private readonly BusyPlayers _busy = new();
        private Phase _phase = Phase.Pausing;
        private int _index = -1;
        private float _resumeAt;
        private StepRun _step;

        public ProgramRun(ProgramKit kit, ProgramPlan plan, int starter)
        {
            _kit = kit;
            _plan = plan;
            _starter = starter;
        }

        private enum Phase
        {
            Pausing,
            Running,
            Done
        }

        public string Name => _plan.Name;

        // Starts over after the last step instead of ending.
        public bool Loops => _plan.Loops;

        public bool IsDone => _phase == Phase.Done;

        public string Status => _phase == Phase.Running
            ? $"{Name} is on step {_index + 1} of {_plan.Steps.Count}, with {_step.Running} fight(s) running."
            : $"{Name} is pausing before step {_index + 1} of {_plan.Steps.Count}.";

        public IEnumerator Body()
        {
            NextStep(_kit.Clock.GameSeconds);
            while (!IsDone)
            {
                yield return null;
                Tick(_kit.Clock.GameSeconds);
            }
        }

        public void Stop()
        {
            _step?.Release();
            _phase = Phase.Done;
        }

        private void Tick(float now)
        {
            if (_phase == Phase.Pausing && now >= _resumeAt) Begin(now);
            else if (_phase == Phase.Running) RunStep(now);
        }

        private void RunStep(float now)
        {
            _step.Update(now);
            if (_step.IsDone) NextStep(now);
        }

        private void NextStep(float now)
        {
            _index++;
            if (_index >= _plan.Steps.Count && Loops) _index = 0;
            if (_index >= _plan.Steps.Count)
            {
                _phase = Phase.Done;
                _kit.Console.Execute($"broadcast Program {Name} is done.");
                return;
            }

            float pause = _plan.Steps[_index].PauseSeconds;
            _kit.Console.Execute($"broadcast Next: step {_index + 1} of {Name} in {Numbers.Format(pause)} s.");
            _phase = Phase.Pausing;
            _resumeAt = now + pause;
        }

        private void Begin(float now)
        {
            _step = new StepRun(_kit, _plan.Steps[_index], _busy);
            _kit.Replies.Send(_starter, $"Step {_index + 1} of {Name}: {_step.Begin(now)}");
            _phase = Phase.Running;
        }
    }
}
