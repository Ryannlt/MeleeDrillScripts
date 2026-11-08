using MDS.Events;
using MDS.Core;

namespace MDS.ConsoleCommands
{
    public class ShootingTrainingCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.ShootingTraining;

        private static bool _enabled = false;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            return true; // Toggle command takes no arguments.
        }

        public void Execute(int playerId, string[] parameters)
        {
            bool newState = !_enabled;

            bool success = EventDispatcher.Trigger(EventEnum.ShootingTraining, new object[] { newState }, out string errorMessage);

            if (!success)
            {
                Logger.Log($"ShootingTraining failed: {errorMessage}", LogLevel.WARNING);
                Feedback.Reply(playerId, errorMessage);
                return;
            }

            // Only commit the new state once the game settings were applied successfully.
            _enabled = newState;
            Logger.Log("ShootingTrainingCommand executed sucessfully", LogLevel.INFO);
            Feedback.Reply(playerId, $"Shooting training is now {(_enabled ? "ON" : "OFF")}.");
        }
    }
}

