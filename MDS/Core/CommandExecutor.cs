using Holdfast.Modding;
using System;
using UnityEngine;

// The CommandExecutor is solely responsible for finding and executing strings to the F1 console.

namespace MDS.Core
{
    public static class CommandExecutor
    {
        // High-frequency commands can fail once per bot per tick, so their warnings share one slot.
        private const float QuietWarningIntervalSeconds = 5f;

        private static IModConsole _console;
        private static float _nextQuietWarning;

        public static void InitializeConsole(IModConsole console)
        {
            _console = console;
            if (_console == null)
            {
                Debug.LogError("[MDS] Console not found.");
                return;
            }

            Debug.Log("[MDS] Console found.");
        }

        // Runs a command and hands back the game's answer. Only an exception is logged, so the caller decides what
        // a refusal means.
        public static bool TryExecute(string command, out string output, int adminId = -1)
        {
            return Run(command, false, adminId, out output);
        }

        // logResult=false marks a high-frequency command so it does not flood the log.
        // adminId attributes the command to that admin in the game's admin feed; -1 leaves it mod-driven.
        public static bool ExecuteCommand(string command, bool logResult = true, int adminId = -1)
        {
            bool success = Run(command, false, adminId, out string output);
            Report(command, success, output, logResult);
            return success;
        }

        // The same, except a 'set' change reverts on the next map load instead of becoming a permanent override.
        public static bool ExecuteForCurrentRound(string command, bool logResult = true, int adminId = -1)
        {
            bool success = Run(command, true, adminId, out string output);
            Report(command, success, output, logResult);
            return success;
        }

        private static bool Run(string command, bool forCurrentRound, int adminId, out string output)
        {
            output = string.Empty;

            if (_console == null)
            {
                Logger.Log("Cannot execute command - Console Input Field is null.", LogLevel.ERROR);
                return false;
            }

            bool success = forCurrentRound
                ? _console.ExecuteForCurrentRound(command, out output, out Exception exception, adminId)
                : _console.Execute(command, out output, out exception, adminId);

            if (exception == null) return success;

            Logger.Log($"Failed to execute command '{command}': {exception}", LogLevel.ERROR);
            return false;
        }

        private static void Report(string command, bool success, string output, bool logResult)
        {
            if (success)
            {
                // DEBUG-only so it's silent in normal play; shows what the game returned and, via the log timestamp, when.
                if (logResult) Logger.Log($"Executed '{command}' -> output='{output}'", LogLevel.DEBUG);
                return;
            }

            if (!logResult)
            {
                float now = Time.realtimeSinceStartup;
                if (now < _nextQuietWarning) return;
                _nextQuietWarning = now + QuietWarningIntervalSeconds;
            }

            Logger.Log($"Command '{command}' failed: '{output}'", LogLevel.WARNING);
        }
    }
}
