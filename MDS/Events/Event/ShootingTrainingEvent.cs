using MDS.Core;

namespace MDS.Events
{
    public class ShootingTrainingEvent : IEvent
    {
        public EventEnum EventName => EventEnum.ShootingTraining;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            return true; // Always valid regardless of parameter count or type. We assume input is already validated.
        }

        public void Trigger(object[] parameters)
        {
            bool enabled = (bool)parameters[0];
            string value = enabled ? "true" : "false";

            Logger.Log($"Executing {EventName} as '{value}'", LogLevel.DEBUG);
            CommandExecutor.ExecuteForCurrentRound($"set characterInfiniteFirearmAmmo {value}");
            CommandExecutor.ExecuteForCurrentRound($"set drawFirearmTrajectories {value}");
        }
    }
}