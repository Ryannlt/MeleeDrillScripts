using System;
using System.Collections;
using MDS.Features.Drills.Settings;
using MDS.Shared.Game;

namespace MDS.Features.Drills
{
    // Counts down on screen, places everyone under god mode, then turns it off again, so nobody is hit while the field moves.
    // A drill that teleports early places everyone as the countdown starts and holds them still until it ends, so they can find their fight.
    // A custom drill can skip the countdown or the god mode.
    // One at a time: two drills would fight over god mode.
    public sealed class DrillCountdown
    {
        private const float CountdownStepSeconds = 1f;

        private readonly GameConsole _console;
        private readonly Routines _routines;
        private readonly PlayerHold _hold;
        private readonly CountdownSetting _seconds;
        private Routine _running;
        private string _title = string.Empty;

        public DrillCountdown(GameConsole console, Routines routines, CountdownSetting seconds)
        {
            _seconds = seconds;
            _console = console;
            _routines = routines;
            _hold = new PlayerHold(console);
        }

        public bool IsRunning => _running != null && !_running.IsFinished;

        public string Busy => $"{_title} is already counting down.";

        // The run's lineup, set by place, is who gets held.
        public void Start(DrillTiming timing, DrillRun run, Action place)
        {
            _title = timing.Title;
            _running = _routines.Start(Run(timing, run, place), timing.Title);
        }

        // A countdown cut short by a new round would otherwise leave its players held.
        public void ReleaseHeld() => _hold.ReleaseAll();

        private IEnumerator Run(DrillTiming timing, DrillRun run, Action place)
        {
            if (timing.PlacesEarly) PlaceAndHold(timing, run, place);
            int seconds = _seconds.Value;
            for (int left = timing.CountsDown ? seconds : 0; left > 0; left--)
            {
                _console.Execute(left == seconds ? $"broadcast {timing.Title} starting in {left}..." : $"broadcast {left}...");
                yield return Routines.Wait(CountdownStepSeconds);
            }

            if (timing.PlacesEarly) _hold.ReleaseAll();
            else Cover(timing, place);
            if (timing.GodModeSeconds <= 0f) yield break;

            yield return Routines.Wait(timing.GodModeSeconds);
            _console.ExecuteForCurrentRound("set characterGodMode false");
        }

        private void PlaceAndHold(DrillTiming timing, DrillRun run, Action place)
        {
            Cover(timing, place);
            if (run.Lineup != null) _hold.Hold(run.Lineup.Everyone());
        }

        private void Cover(DrillTiming timing, Action place)
        {
            if (timing.GodModeSeconds > 0f) _console.ExecuteForCurrentRound("set characterGodMode true");
            place();
        }
    }
}
