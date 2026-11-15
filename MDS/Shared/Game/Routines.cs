using System;
using System.Collections;
using System.Collections.Generic;
using MDS.Shared.Logging;

// Delayed work, stepped from OnUpdateElapsedTime. Engine coroutines would need a MonoBehaviour kept alive across map
// changes and run outside the game's mod guard; these stop with the mod, and a failing one is logged and dropped.

namespace MDS.Shared.Game
{
    public sealed class Routine
    {
        internal Routine(IEnumerator body, string name)
        {
            Body = body;
            Name = name;
        }

        public bool IsFinished { get; internal set; }

        internal IEnumerator Body { get; }

        internal string Name { get; }

        internal float WakeAt { get; set; }
    }

    public sealed class Routines
    {
        private sealed class SecondsWait
        {
            public SecondsWait(float seconds)
            {
                Seconds = seconds;
            }

            public float Seconds { get; }
        }

        private readonly List<Routine> _routines = new();
        private readonly ModLog _log;
        private readonly IClock _clock;
        private bool _ticking;

        public Routines(ModLog log, IClock clock)
        {
            _log = log;
            _clock = clock;
        }

        // In a routine, `yield return null` waits one frame and `yield return Routines.Wait(s)` waits s seconds.
        public static object Wait(float seconds) => new SecondsWait(seconds);

        // Runs from the next tick on.
        public Routine Start(IEnumerator body, string name)
        {
            var routine = new Routine(body, name);
            _routines.Add(routine);
            return routine;
        }

        public void Stop(Routine routine)
        {
            if (routine != null) routine.IsFinished = true;
        }

        public void StopAll()
        {
            foreach (Routine routine in _routines) routine.IsFinished = true;
            if (!_ticking) _routines.Clear();
        }

        public void Tick()
        {
            float now = _clock.GameSeconds;
            // Counted first, so a routine started by another's step waits for the next tick.
            int count = _routines.Count;

            _ticking = true;
            for (int i = 0; i < count; i++) StepIfDue(_routines[i], now);
            _ticking = false;

            _routines.RemoveAll(routine => routine.IsFinished);
        }

        private void StepIfDue(Routine routine, float now)
        {
            if (routine.IsFinished || now < routine.WakeAt) return;

            try
            {
                routine.IsFinished = !routine.Body.MoveNext();
                if (!routine.IsFinished) routine.WakeAt = routine.Body.Current is SecondsWait wait ? now + wait.Seconds : 0f;
            }
            catch (Exception e)
            {
                routine.IsFinished = true;
                _log.Error($"Routine '{routine.Name}' failed and was dropped: {e}");
            }
        }
    }
}
