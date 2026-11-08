using System.Linq;
using MDS.Core;
using MDS.Events;

namespace MDS.ConsoleCommands
{
    public class TestCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.Test;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;
            return true; // Accepts any input
        }

        public void Execute(int playerId, string[] parameters)
        {
            bool success = EventDispatcher.Trigger(EventEnum.TestEvent, parameters.Cast<object>().ToArray(), out string errorMessage);

            if (!success)
            {
                Logger.Log($"TestEvent failed: {errorMessage}", LogLevel.WARNING);
                Feedback.Reply(playerId, errorMessage);
                return;
            }

            Logger.Log("TestEvent executed successfully.", LogLevel.INFO);
        }
    }
}
