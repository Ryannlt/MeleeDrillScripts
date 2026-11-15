using System;
using System.Collections.Generic;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.ShootingTraining
{
    public sealed class ShootingTrainingCommand : IConsoleCommand
    {
        private static readonly Dictionary<string, bool> States = new(StringComparer.OrdinalIgnoreCase)
        {
            { "on", true }, { "true", true }, { "off", false }, { "false", false },
        };

        private readonly ShootingTrainingMode _mode;
        private readonly Replies _replies;

        public ShootingTrainingCommand(ShootingTrainingMode mode, Replies replies)
        {
            _mode = mode;
            _replies = replies;
        }

        public string Name => "shootingTraining";

        public bool Validate(string[] parameters, out string errorMessage)
        {
            bool valid = parameters.Length == 0 || (parameters.Length == 1 && States.ContainsKey(parameters[0]));
            errorMessage = valid ? string.Empty : "Usage: rc shootingTraining [on|off]";
            return valid;
        }

        public void Execute(int playerId, string[] parameters)
        {
            bool on = parameters.Length == 0 ? !_mode.IsOn : States[parameters[0]];
            string reply = _mode.TrySet(on)
                ? $"Shooting training is now {(on ? "ON" : "OFF")}."
                : $"The game refused a setting, so shooting training stays {(_mode.IsOn ? "ON" : "OFF")}.";
            _replies.Send(playerId, reply);
        }
    }
}
