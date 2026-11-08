using System.Linq;
using MDS.Core;

namespace MDS.Events
{
    public class TestEvent : IEvent
    {
        public EventEnum EventName => EventEnum.TestEvent;

        public bool Validate(object[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            return true; // Always valid regardless of parameter count or type
        }

        public void Trigger(object[] parameters)
        {
            string message = parameters.Length > 0
                ? string.Join(" ", parameters.Select(p => p?.ToString() ?? "null"))
                : "Test event triggered with no parameters.";

            Logger.Log($"Executing {EventName} with message: {message}", LogLevel.DEBUG);
            CommandExecutor.ExecuteCommand($"broadcast Test: {message}");
        }
    }
}
