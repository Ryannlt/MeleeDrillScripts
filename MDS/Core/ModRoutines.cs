using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Delayed work, stepped from OnUpdateElapsedTime. Engine coroutines would need a MonoBehaviour kept alive across map
// changes and run outside the game's mod guard; these stop with the mod, and a failing one is logged and dropped.

namespace MDS.Core
{
    public sealed class ModRoutine
    {
        internal readonly IEnumerator Body;
        internal readonly string Name;
        internal float WakeAt;

        public bool IsFinished { get; internal set; }

        internal ModRoutine(IEnumerator body, string name)
        {
            Body = body;
            Name = name;
        }
    }

    public static class ModRoutines
    {
        private sealed class SecondsWait
        {
            public readonly float Seconds;
            public SecondsWait(float seconds) { Seconds = seconds; }
        }

        private static readonly List<ModRoutine> _routines = new();
        private static bool _ticking;

        // Runs from the next frame on. In a routine, `yield return null` waits one frame.
        public static ModRoutine Start(IEnumerator body, string name)
        {
            var routine = new ModRoutine(body, name);
            _routines.Add(routine);
            return routine;
        }

        public static object Wait(float seconds) => new SecondsWait(seconds);

        public static void Stop(ModRoutine routine)
        {
            if (routine != null) routine.IsFinished = true;
        }

        public static void StopAll()
        {
            for (int i = 0; i < _routines.Count; i++)
                _routines[i].IsFinished = true;

            if (!_ticking) _routines.Clear();
        }

        public static void Tick()
        {
            float now = Time.time;

            // Counted first, so a routine started by another's step waits for the next frame.
            int count = _routines.Count;

            _ticking = true;
            for (int i = 0; i < count; i++)
            {
                ModRoutine routine = _routines[i];
                if (!routine.IsFinished && now >= routine.WakeAt) Step(routine, now);
            }
            _ticking = false;

            _routines.RemoveAll(r => r.IsFinished);
        }

        private static void Step(ModRoutine routine, float now)
        {
            try
            {
                if (!routine.Body.MoveNext())
                {
                    routine.IsFinished = true;
                    return;
                }

                routine.WakeAt = routine.Body.Current is SecondsWait wait ? now + wait.Seconds : 0f;
            }
            catch (Exception e)
            {
                routine.IsFinished = true;
                Logger.Log($"Routine '{routine.Name}' failed and was dropped: {e}", LogLevel.ERROR);
            }
        }
    }
}
