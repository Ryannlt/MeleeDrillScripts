using System;
using MDS.Core;
using MDS.Systems;

namespace MDS.ConsoleCommands
{
    // rc meleelog [on|off] - the melee debug mode. rc meleelog frames <on|off> adds the frame buffer.
    public class MeleeLogCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.MeleeLog;

        private const string Usage = "Usage: rc meleelog [on|off] or rc meleelog frames <on|off>";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            bool valid = parameters.Length == 0
                || (parameters.Length == 1 && IsOnOff(parameters[0]))
                || (parameters.Length == 2 && Is(parameters[0], "frames") && IsOnOff(parameters[1]));

            if (!valid) errorMessage = Usage;
            return valid;
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (parameters.Length == 2)
            {
                MeleeDebug.SetFramesEnabled(Is(parameters[1], "on"));
                if (MeleeDebug.FramesEnabled) MeleeDebug.SetEnabled(true);
                Feedback.Reply(playerId, $"Melee log frames are {(MeleeDebug.FramesEnabled ? "ON" : "OFF")}. Watch the server log.");
                return;
            }

            MeleeDebug.SetEnabled(parameters.Length == 1 ? Is(parameters[0], "on") : !MeleeDebug.Enabled);
            Feedback.Reply(playerId, $"Melee log is {(MeleeDebug.Enabled ? "ON" : "OFF")}. Watch the server log.");
        }

        private static bool IsOnOff(string value) => Is(value, "on") || Is(value, "off");

        private static bool Is(string value, string expected) => value.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }
}
