using System;
using Holdfast.Modding;
using MDS.Shared.Logging;

namespace MDS.Shared.Game
{
    public sealed class GameConsole
    {
        public const int ModDriven = -1;

        // High-frequency commands can fail every tick, so their warnings share one slot.
        private const float QuietWarningIntervalSeconds = 5f;

        private readonly IModConsole _console;
        private readonly ModLog _log;
        private readonly IClock _clock;
        private float _nextQuietWarning;

        public GameConsole(IModConsole console, ModLog log, IClock clock)
        {
            _console = console;
            _log = log;
            _clock = clock;
            if (_console == null) _log.Error("The game handed over no console.");
        }

        // Only an exception is logged, so the caller decides what a refusal means.
        public bool TryExecute(string command, out string output, int adminId = ModDriven)
        {
            var result = Run(command, false, adminId);
            output = result.Output;
            return result.Success;
        }

        // logResult false is for high-frequency commands, so their failures don't flood the log.
        // adminId credits the command to that admin in the game's admin feed.
        public bool Execute(string command, bool logResult = true, int adminId = ModDriven)
        {
            var result = Run(command, false, adminId);
            Report(command, result, logResult);
            return result.Success;
        }

        // A 'set' made this way reverts on the next map load instead of becoming a permanent override.
        public bool ExecuteForCurrentRound(string command, bool logResult = true, int adminId = ModDriven)
        {
            var result = Run(command, true, adminId);
            Report(command, result, logResult);
            return result.Success;
        }

        private (bool Success, string Output) Run(string command, bool forCurrentRound, int adminId)
        {
            if (_console == null) return (false, string.Empty);

            bool success = forCurrentRound
                ? _console.ExecuteForCurrentRound(command, out string output, out Exception exception, adminId)
                : _console.Execute(command, out output, out exception, adminId);
            if (exception == null) return (success, output);

            _log.Error($"Command '{command}' threw: {exception}");
            return (false, output);
        }

        private void Report(string command, (bool Success, string Output) result, bool logResult)
        {
            if (result.Success)
            {
                if (logResult) _log.Debug($"Executed '{command}' -> output='{result.Output}'");
                return;
            }
            if (!logResult && !QuietWarningDue()) return;

            _log.Warning($"Command '{command}' failed: '{result.Output}'");
        }

        private bool QuietWarningDue()
        {
            float now = _clock.RealSeconds;
            if (now < _nextQuietWarning) return false;

            _nextQuietWarning = now + QuietWarningIntervalSeconds;
            return true;
        }
    }
}
