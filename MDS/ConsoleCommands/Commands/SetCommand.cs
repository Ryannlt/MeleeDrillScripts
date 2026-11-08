using System;
using MDS.ConfigVariables;

namespace MDS.ConsoleCommands
{
    public class SetCommand : IConsoleCommand
    {
        public ConsoleCommandEnum CommandName => ConsoleCommandEnum.Set;

        public bool Validate(string[] parameters, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (parameters.Length < 1)
            {
                errorMessage = "Missing set key. Usage: rc set <key> [value]";
                return false;
            }

            if (!Enum.TryParse(parameters[0], true, out ConfigurableEnum parsedKey))
            {
                // Silent failure for unknown keys like the game’s own `rc set` usage
                return false;
            }

            if (!ConfigurableRegistry.TryGet(parsedKey, out var configurable))
            {
                errorMessage = $"Configurable '{parsedKey}' is not registered.";
                return false;
            }

            string[] args = parameters.Length > 1 ? parameters[1..] : Array.Empty<string>();
            return configurable.ValidateSet(args, out errorMessage);
        }

        public void Execute(int playerId, string[] parameters)
        {
            if (!Enum.TryParse(parameters[0], true, out ConfigurableEnum parsedKey)) return;

            if (!ConfigurableRegistry.TryGet(parsedKey, out var configurable)) return;

            string[] args = parameters.Length > 1 ? parameters[1..] : Array.Empty<string>();
            configurable.Set(playerId, args);
        }
    }
}
