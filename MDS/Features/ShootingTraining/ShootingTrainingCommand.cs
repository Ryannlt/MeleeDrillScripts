using MDS.Shared;
using MDS.Shared.Commands;
using MDS.Shared.Game;

namespace MDS.Features.ShootingTraining
{
    public sealed class ShootingTrainingCommand : IConsoleCommand
    {
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
            bool valid = parameters.Length == 0 || (parameters.Length == 1 && OnOff.TryParse(parameters[0], out _));
            errorMessage = valid ? string.Empty : "Usage: rc shootingTraining [on|off]";
            return valid;
        }

        public void Execute(int playerId, string[] parameters)
        {
            bool on = parameters.Length == 0 ? !_mode.IsOn : OnOff.TryParse(parameters[0], out bool given) && given;
            string reply = _mode.TrySet(on)
                ? $"Shooting training is now {(on ? "ON" : "OFF")}."
                : $"The game refused a setting, so shooting training stays {(_mode.IsOn ? "ON" : "OFF")}.";
            _replies.Send(playerId, reply);
        }
    }
}
