using System;
using System.Collections;
using MDS.Shared.Game;

namespace MDS.Features.Drills
{
    // Counts down on screen, places everyone under god mode, then turns it off again, so nobody is hit while the field moves.
    // A custom drill can skip the countdown or the god mode.
    // One at a time: two drills would fight over god mode.
    public sealed class DrillCountdown
    {
        private const int CountdownSeconds = 3;
        private const float CountdownStepSeconds = 1f;

        private readonly GameConsole _console;
        private readonly Routines _routines;
        private Routine _running;
        private string _title = string.Empty;

        public DrillCountdown(GameConsole console, Routines routines)
        {
            _console = console;
            _routines = routines;
        }

        public bool IsRunning => _running != null && !_running.IsFinished;

        public string Busy => $"{_title} is already counting down.";

        public void Start(DrillTiming timing, Action place)
        {
            _title = timing.Title;
            _running = _routines.Start(Run(timing, place), timing.Title);
        }

        private IEnumerator Run(DrillTiming timing, Action place)
        {
            for (int left = timing.CountsDown ? CountdownSeconds : 0; left > 0; left--)
            {
                _console.Execute(left == CountdownSeconds ? $"broadcast {timing.Title} starting in {left}..." : $"broadcast {left}...");
                yield return Routines.Wait(CountdownStepSeconds);
            }

            bool covered = timing.GodModeSeconds > 0f;
            if (covered) _console.ExecuteForCurrentRound("set characterGodMode true");
            place();
            if (!covered) yield break;

            yield return Routines.Wait(timing.GodModeSeconds);
            _console.ExecuteForCurrentRound("set characterGodMode false");
        }
    }
}
