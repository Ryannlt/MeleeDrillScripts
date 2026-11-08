using System.Collections.Generic;
using UnityEngine;
using MDS.ConfigVariables;

namespace MDS.Systems
{
    // StabbingDummy AI: stands where it spawned and stabs on a fixed cadence, for block practice.
    public class MeleeDummy : IBotAi, IConfigurableAi
    {
        public enum StabDirection { Random, High, Low, Alternate }

        private const float WindupSeconds = 0.15f;   // hold the windup this long (one MeleeStrike) before releasing
        private const float FirstStabDelay = 1.0f;   // settle after spawning before the first stab

        // Built-in lever defaults, the single source for these values.
        public static readonly (string name, string value)[] DefaultLevers =
        {
            ("stabInterval", "1.7"),    // seconds from release to the next windup, above the ~1.5s stab recovery
            ("stabDirection", "Random"),
        };

        // Tunable levers (see IConfigurableAi). Seeded from global defaults in the constructor.
        private float _stabInterval;                 // seconds from release to the next windup
        private StabDirection _stabDirection;        // which way each stab goes

        private bool _stancePending = true;   // issue EnableCombatStance once, on the first spawned tick
        private bool _windingUp;              // a stab is chambered, waiting to release
        private float _executeAt;             // realtime to release the current windup
        private float _nextWindupAt;          // realtime the next stab may begin
        private bool _lastHigh;               // toggles for StabDirection.Alternate

        public BotAiEnum AiType => BotAiEnum.StabbingDummy;

        public MeleeDummy()
        {
            // Each lever takes its global default if set, otherwise the built-in fallback; TrySet parses it. A
            // bad global default just falls back to the built-in.
            string ai = AiType.ToString();
            foreach (var (name, builtin) in DefaultLevers)
                if (!TrySet(name, GlobalAiConfigurable.Default(ai, name, builtin), out _))
                    TrySet(name, builtin, out _);
        }

        public BotIntent Decide(BotController self, float deltaTime)
        {
            if (!self.TryGetPose(out _)) return BotIntent.Idle; // not currently spawned - issue nothing

            float now = Time.realtimeSinceStartup;

            // One-time: enter combat stance so the dummy can strike. Consumed only once actually spawned.
            if (_stancePending)
            {
                _stancePending = false;
                _nextWindupAt = now + FirstStabDelay;
                return new BotIntent { Action = "EnableCombatStance", MoveAxis = Vector2.zero };
            }

            // Stand still. We never send a look command - the engine holds the spawn facing set in OnSpawned, so
            // the dummy keeps looking one way and its stabs go straight ahead.
            BotIntent intent = new BotIntent { MoveAxis = Vector2.zero };

            if (_windingUp)
            {
                if (now >= _executeAt)
                {
                    intent.Action = "ExecuteMeleeWeaponStrike"; // release; a single Execute ends the swing cleanly
                    _windingUp = false;
                    _nextWindupAt = now + _stabInterval;
                }
                // else: hold the windup silently (re-sending MeleeStrike would restart the windup animation)
            }
            else if (now >= _nextWindupAt)
            {
                _executeAt = now + WindupSeconds;
                _windingUp = true;
                intent.Action = "MeleeStrike" + NextStabDir(); // one windup command; do not repeat it
            }

            return intent;
        }

        // "High" or "Low" for the next stab, per the configured direction.
        private string NextStabDir()
        {
            switch (_stabDirection)
            {
                case StabDirection.High: return "High";
                case StabDirection.Low:  return "Low";
                case StabDirection.Alternate: _lastHigh = !_lastHigh; return _lastHigh ? "High" : "Low";
                default: return Random.value < 0.5f ? "High" : "Low"; // Random
            }
        }

        public bool TrySet(string name, string value, out string error)
        {
            error = string.Empty;
            switch (name.ToLowerInvariant())
            {
                case "stabinterval":
                    if (!float.TryParse(value, out float interval) || interval <= 0f)
                    {
                        error = "stabInterval must be a number > 0 (seconds between stabs).";
                        return false;
                    }
                    _stabInterval = interval;
                    return true;

                case "stabdirection":
                    if (!System.Enum.TryParse(value, true, out StabDirection dir))
                    {
                        error = "stabDirection must be one of: Random, High, Low, Alternate.";
                        return false;
                    }
                    _stabDirection = dir;
                    return true;

                default:
                    error = $"Unknown lever '{name}'. StabbingDummy levers: stabInterval, stabDirection.";
                    return false;
            }
        }

        // Both levers are always live: a stabbing dummy has no capability toggles for them to depend on.
        public List<(string name, string value, string inactive)> ListParams()
        {
            return new List<(string name, string value, string inactive)>
            {
                ("stabInterval", _stabInterval.ToString("0.##"), null),
                ("stabDirection", _stabDirection.ToString(), null),
            };
        }

        // Carry the dummy's config to a Replace-policy replacement so its tuning isn't lost on death.
        public void InheritFrom(IBotAi previous)
        {
            if (previous is MeleeDummy d)
            {
                _stabInterval = d._stabInterval;
                _stabDirection = d._stabDirection;
            }
        }
    }
}
