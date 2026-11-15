using System;
using System.Collections;
using MDS.Shared.Game;

namespace MDS.Features.Drills
{
    // Counts down on screen, places everyone under god mode, then turns it off again, so nobody is hit while the field moves.
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

        public void Start(string title, Action place, float godModeSeconds)
        {
            _title = title;
            _running = _routines.Start(Run(title, place, godModeSeconds), title);
        }

        private IEnumerator Run(string title, Action place, float godModeSeconds)
        {
            for (int left = CountdownSeconds; left > 0; left--)
            {
                _console.Execute(left == CountdownSeconds ? $"broadcast {title} starting in {left}..." : $"broadcast {left}...");
                yield return Routines.Wait(CountdownStepSeconds);
            }

            _console.ExecuteForCurrentRound("set characterGodMode true");
            place();
            yield return Routines.Wait(godModeSeconds);
            _console.ExecuteForCurrentRound("set characterGodMode false");
        }
    }
}
